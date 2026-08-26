export const environment = {
  production: false,
  /**
   * Same-origin in development too: `ng serve` proxies /api to the .NET host
   * (see proxy.conf.json), so the browser never makes a cross-origin request
   * and the API needs no CORS configuration.
   */
  apiUrl: '/api',
};
