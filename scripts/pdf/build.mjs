// Gera entregaveis/Documentacao-completa.pdf e entregaveis/Resumo-executivo.pdf a partir dos .md (fonte de verdade).
// Uso: cd scripts/pdf && npm install && npx playwright install chromium && npm run pdf
import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import { marked } from "marked";
import { chromium } from "playwright";

const SRC = process.argv[2];           // raiz do repositório (snapshot)
const OUT = process.argv[3];           // pasta de saída
const HERE = path.dirname(new URL(import.meta.url).pathname);
const MERMAID = fs.readFileSync(path.join(HERE, "node_modules/mermaid/dist/mermaid.min.js"), "utf8");

const TITLE = "Pedidos e Catálogo – Evolução arquitetural";
const SUB = "Desafio Técnico · COE Sênior – Arquitetura de Soluções · Stefanini";
const DATE = "Outubro de 2026";

const slug = (s) => s.trim().toLowerCase().replace(/[^\p{L}\p{N}_\- ]/gu, "").replace(/ /g, "-");
const docId = (rel) => "d-" + rel.replace(/\.md$/, "").replace(/[\/.]/g, "-");
const esc = (s) => s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");

// ---------- Estrutura da documentação completa ----------
const PARTS = [
  { title: "Parte I – Visão geral", docs: ["README.md", "docs/00-resumo-executivo.md"] },
  { title: "Parte II – Arquitetura, domínio e decisões", docs: ["docs/01-diagnostico.md", "docs/02-arquitetura.md", "docs/03-dominios-e-integracao.md", "docs/adr/README.md",
      ...fs.readdirSync(path.join(SRC, "docs/adr")).filter(f => f.startsWith("ADR-")).sort().map(f => "docs/adr/" + f)] },
  { title: "Parte III – Qualidade, segurança e evolução", docs: ["docs/04-qualidade-e-resiliencia.md", "docs/05-threat-model.md", "docs/06-plano-30-60-90.md", "docs/07-riscos-premissas-escopo.md"] },
  { title: "Parte IV – IA, governança e estimativa", docs: ["docs/uso-de-ia.md", "docs/08-ia-consulta-de-pedidos.md", "docs/ia/README.md", "docs/ia/prompt-de-sistema.md", "docs/ia/casos-de-avaliacao.jsonl",
      "docs/09-governanca-de-contratos.md", "docs/excecoes.md", "docs/10-estimativa-fase-30-dias.md"] },
  { title: "Anexo – Rastreabilidade", docs: ["docs/rastreabilidade.md"] },
];
const INCLUDED = new Set(PARTS.flatMap(p => p.docs));

// README: no PDF, só as seções úteis para leitura (índice de arquivos e estrutura são do repositório)
function readDoc(rel) {
  let t = fs.readFileSync(path.join(SRC, rel), "utf8").replace(/\r\n/g, "\n");
  if (rel === "README.md") {
    t = t.replace(/\n## 7\. Índice dos artefatos[\s\S]*$/, "\n");
    t = t.replace("# Pedidos e Catálogo – evolução arquitetural", "# Como ler este documento");
    t = t.replace(/> \*\*Por onde começar:\*\*.*\n/, "> **Por onde começar:** leia \"Em 1 minuto\", depois o resumo executivo (2 páginas). Para avaliar, use o roteiro por critério (seção 3). Para conferir item por item do PDF, use o anexo de rastreabilidade. Este PDF é gerado a partir dos arquivos `.md` do repositório, que são a fonte.\n");
  }
  if (rel.endsWith(".jsonl")) {
    const rows = t.trim().split("\n").map(l => JSON.parse(l));
    t = "# Casos de avaliação do assistente (semente)\n\nSemente da suíte offline. Casos bloqueantes reprovam com um único erro.\n\n" +
      "| Id | Categoria | Pergunta | Comportamento esperado | Bloqueante |\n|---|---|---|---|---|\n" +
      rows.map(r => `| ${r.id} | ${r.categoria.replace(/_/g, " ")} | ${r.pergunta.replace(/\|/g, "\\|")} | ${r.esperado.replace(/\|/g, "\\|")} | ${r.bloqueante ? "sim" : "não"} |`).join("\n") + "\n";
  }
  return t;
}

// Renderiza um documento com ids prefixados e links reescritos
function renderDoc(rel, mode) {
  const id = docId(rel);
  const counts = {};
  const renderer = new marked.Renderer();
  renderer.heading = function ({ tokens, depth, text }) {
    let s = slug(text.replace(/<[^>]+>/g, ""));
    if (s in counts) { counts[s]++; s = `${s}-${counts[s]}`; } else counts[s] = 0;
    const inner = this.parser.parseInline(tokens);
    const lvl = Math.min(depth + 1, 6); // h1 do .md vira h2 (capítulo); h1 fica para as partes
    const cls = depth === 1 ? ' class="chapter-title"' : "";
    return `<h${lvl} id="${id}--${s}"${cls}>${inner}</h${lvl}>\n`;
  };
  renderer.link = function ({ href, tokens }) {
    const inner = this.parser.parseInline(tokens);
    if (/^(https?:|mailto:)/.test(href)) return `<a href="${href}">${inner}</a>`;
    const [p, frag] = href.split("#");
    let target = p ? path.posix.normalize(path.posix.join(path.posix.dirname(rel), p)) : rel;
    if (target.endsWith("/")) target = target.replace(/\/$/, "");
    if (target === "docs/ia") target = "docs/ia/README.md";
    if (mode === "full" && INCLUDED.has(target)) {
      return `<a href="#${docId(target)}${frag ? "--" + frag : ""}">${inner}</a>`;
    }
    // Arquivo que não está no PDF (código, contrato, CI) ou resumo isolado: mostra o caminho no repositório
    const repoPath = target.endsWith(".md") ? null : target;
    return repoPath ? `${inner} <span class="repo">(${esc(repoPath)})</span>` : `${inner}`;
  };
  renderer.code = function ({ text, lang }) {
    if (lang === "mermaid") return `<div class="diagram"><pre class="mermaid">${esc(text)}</pre></div>`;
    return `<pre class="code"><code>${esc(text)}</code></pre>`;
  };
  renderer.blockquote = function ({ tokens }) {
    const body = this.parser.parse(tokens);
    const cls = body.includes("Para o avaliador") ? "callout" : "quote";
    return `<blockquote class="${cls}">${body}</blockquote>`;
  };
  renderer.table = function (token) {
    const head = token.header.map((c, i) => `<th${token.align[i] ? ` style="text-align:${token.align[i]}"` : ""}>${this.parser.parseInline(c.tokens)}</th>`).join("");
    const body = token.rows.map(r => "<tr>" + r.map((c, i) => `<td${token.align[i] ? ` style="text-align:${token.align[i]}"` : ""}>${this.parser.parseInline(c.tokens)}</td>`).join("") + "</tr>").join("");
    const wide = token.header.length >= 5 ? " wide" : "";
    return `<div class="tbl${wide}"><table><thead><tr>${head}</tr></thead><tbody>${body}</tbody></table></div>`;
  };
  marked.use({ gfm: true, breaks: false });
  let md = readDoc(rel);
  if (mode === "summary") md = md.replace(/\n## Índice[\s\S]*$/, "\n").replace(/\nDetalhe em \[10-[^\n]*\n/, "\n");
  return `<section class="doc" id="${id}">${marked.parse(md, { renderer })}</section>`;
}

const CSS = `
@page { size: A4; margin: 18mm 16mm 18mm 16mm; }
:root { --ink:#1b1f24; --muted:#5b6470; --line:#d9dee4; --accent:#0b4f8a; --soft:#f3f6f9; --callout:#eef4fb; }
* { box-sizing: border-box; }
html { font-family: Inter, "DejaVu Sans", sans-serif; font-size: 9.6pt; color: var(--ink); line-height: 1.45; }
body { margin: 0; }
h1, h2, h3, h4, h5 { line-height: 1.25; color: var(--ink); break-after: avoid; }
h1.part { font-size: 22pt; color: var(--accent); margin: 0; padding-top: 70mm; font-weight: 700; }
.part-page { break-before: page; break-after: page; min-height: 200mm; }
.part-page p { color: var(--muted); font-size: 11pt; margin-top: 6mm; }
h2.chapter-title { font-size: 16pt; font-weight: 700; margin: 0 0 4mm; padding-bottom: 2mm; border-bottom: 2px solid var(--accent); }
section.doc { break-before: page; }
section.doc.first { break-before: auto; }
h3 { font-size: 12pt; margin: 6mm 0 2mm; color: var(--accent); }
h4 { font-size: 10.5pt; margin: 4mm 0 1.5mm; }
p, ul, ol { margin: 0 0 2.4mm; }
ul, ol { padding-left: 5mm; }
li { margin-bottom: 0.8mm; }
a { color: var(--accent); text-decoration: none; }
code { font-family: "DejaVu Sans Mono", monospace; font-size: 0.86em; background: var(--soft); padding: 0 0.6mm; border-radius: 1mm; }
pre.code { background: var(--soft); border: 1px solid var(--line); border-radius: 1.5mm; padding: 2.5mm 3mm; white-space: pre-wrap; font-size: 8pt; break-inside: avoid; }
pre.code code { background: none; padding: 0; }
.repo { color: var(--muted); font-family: "DejaVu Sans Mono", monospace; font-size: 0.82em; }
blockquote { margin: 0 0 4mm; }
blockquote.callout { background: var(--callout); border-left: 3px solid var(--accent); padding: 2.5mm 3.5mm; border-radius: 0 1.5mm 1.5mm 0; break-inside: avoid; }
blockquote.callout p { margin: 0; }
blockquote.quote { border-left: 3px solid var(--line); padding: 0.5mm 3.5mm; color: var(--muted); }
.tbl { margin: 1.5mm 0 3.5mm; }
table { border-collapse: collapse; width: 100%; font-size: 8.4pt; }
.tbl.wide table { font-size: 7.8pt; }
th, td { border: 1px solid var(--line); padding: 1.3mm 1.8mm; vertical-align: top; text-align: left; }
th { background: var(--soft); font-weight: 600; }
tr { break-inside: avoid; }
thead { display: table-header-group; }
.diagram { margin: 2mm 0 4mm; text-align: center; break-inside: avoid; }
.diagram svg { max-width: 100% !important; height: auto; max-height: 150mm; }
td code { overflow-wrap: anywhere; word-break: break-word; }
/* capa e sumário */
.cover { height: 255mm; display: flex; flex-direction: column; justify-content: center; }
.cover .kicker { color: var(--accent); font-weight: 600; letter-spacing: 0.04em; text-transform: uppercase; font-size: 9pt; }
.cover h1 { font-size: 28pt; margin: 4mm 0 3mm; }
.cover .sub { font-size: 12pt; color: var(--muted); }
.cover .meta { margin-top: 18mm; font-size: 9.5pt; color: var(--muted); border-top: 1px solid var(--line); padding-top: 4mm; }
.toc { break-before: page; }
.toc h2 { font-size: 16pt; border-bottom: 2px solid var(--accent); padding-bottom: 2mm; }
.toc .tp { font-weight: 700; margin: 4mm 0 1mm; color: var(--accent); }
.toc ol { list-style: none; padding-left: 4mm; margin: 0; }
.toc li { margin: 0.6mm 0; }
/* resumo */
.summary-head { display: flex; justify-content: space-between; color: var(--muted); font-size: 8.5pt; border-bottom: 1px solid var(--line); padding-bottom: 2mm; margin-bottom: 4mm; }
body.summary html { font-size: 9.2pt; }
`;

function chapterTitle(rel) {
  const md = readDoc(rel);
  const m = md.match(/^# (.+)$/m);
  return m ? m[1] : rel;
}

function fullHtml() {
  let toc = `<div class="toc"><h2>Sumário</h2>`;
  let body = "";
  PARTS.forEach((part, i) => {
    const pid = "part-" + (i + 1);
    toc += `<div class="tp"><a href="#${pid}">${part.title}</a></div><ol>`;
    body += `<div class="part-page" id="${pid}"><h1 class="part">${part.title}</h1></div>`;
    part.docs.forEach((rel, j) => {
      toc += `<li><a href="#${docId(rel)}">${esc(chapterTitle(rel))}</a></li>`;
      body += renderDoc(rel, "full").replace('<section class="doc"', j === 0 ? '<section class="doc first"' : '<section class="doc"');
    });
    toc += `</ol>`;
  });
  toc += `</div>`;
  const cover = `<div class="cover"><div class="kicker">Documentação completa</div><h1>${TITLE}</h1><div class="sub">${SUB}</div>
    <div class="meta">${DATE}<br>Gerado a partir dos arquivos <code>.md</code> do repositório (fonte de verdade). Prova executável: <code>dotnet test</code> na raiz do repositório.</div></div>`;
  return page(cover + toc + body, "full");
}

function summaryHtml() {
  const head = `<div class="summary-head"><span>${SUB}</span><span>${DATE}</span></div>`;
  return page(head + renderDoc("docs/00-resumo-executivo.md", "summary").replace('<section class="doc"', '<section class="doc first"'), "summary");
}

function page(inner, kind) {
  return `<!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><title>${TITLE}</title><style>${CSS}</style></head>
<body class="${kind}">${inner}
<script>${MERMAID}</script>
<script>
  mermaid.initialize({ startOnLoad:false, theme:"neutral", securityLevel:"loose", fontFamily:"Inter, DejaVu Sans, sans-serif",
    flowchart:{ htmlLabels:true, useMaxWidth:true }, sequence:{ useMaxWidth:true }, gantt:{ useMaxWidth:true } });
  mermaid.run({ querySelector: "pre.mermaid" }).then(() => { window.__done = true; }).catch(e => { window.__err = String(e); window.__done = true; });
</script></body></html>`;
}

const browser = await chromium.launch(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : {});
async function render(html, file, footerLabel) {
  const htmlPath = path.join(os.tmpdir(), file.replace(/\.pdf$/, ".html"));
  fs.writeFileSync(htmlPath, html);
  const pg = await browser.newPage();
  await pg.goto("file://" + htmlPath);
  await pg.waitForFunction(() => window.__done === true, null, { timeout: 120000 });
  const err = await pg.evaluate(() => window.__err);
  if (err) throw new Error("mermaid: " + err);
  await pg.pdf({
    path: path.join(OUT, file), format: "A4", printBackground: true, preferCSSPageSize: true,
    displayHeaderFooter: true, headerTemplate: "<span></span>",
    footerTemplate: `<div style="width:100%;font-family:Inter,sans-serif;font-size:7.5pt;color:#5b6470;padding:0 16mm;display:flex;justify-content:space-between;"><span>${footerLabel}</span><span><span class="pageNumber"></span> / <span class="totalPages"></span></span></div>`,
  });
  await pg.close();
}
await render(fullHtml(), "Documentacao-completa.pdf", "Pedidos e Catálogo – Documentação completa");
await render(summaryHtml(), "Resumo-executivo.pdf", "Pedidos e Catálogo – Resumo executivo");
await browser.close();
console.log("ok");
