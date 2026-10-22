namespace Pedidos.Api;

// Swagger UI via CDN, lendo os contratos OpenAPI publicados em /contracts.
// O interceptor troca o host dos "servers" fictícios do contrato pelo host atual.
internal static class SwaggerPage
{
    public const string Html = """
        <!doctype html>
        <html>
        <head>
          <meta charset="utf-8">
          <title>Pedidos API</title>
          <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css">
        </head>
        <body>
          <div id="ui"></div>
          <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
          <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-standalone-preset.js"></script>
          <script>
            SwaggerUIBundle({
              dom_id: '#ui',
              presets: [SwaggerUIBundle.presets.apis, SwaggerUIStandalonePreset],
              layout: 'StandaloneLayout',
              urls: [
                { url: '/contracts/orders-v1.yaml', name: 'Pedidos v1' },
                { url: '/contracts/orders-v2.yaml', name: 'Pedidos v2' }
              ],
              requestInterceptor: req => {
                if (req.url.includes('/v1/') || req.url.includes('/v2/')) {
                  const path = new URL(req.url).pathname + new URL(req.url).search;
                  req.url = location.origin + path;
                }
                return req;
              }
            });
          </script>
        </body>
        </html>
        """;
}
