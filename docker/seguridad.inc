# Cabeceras de seguridad. Se incluye desde cada `location` que sirve HTML: nginx
# no acumula add_header entre niveles, asi que declararlas solo en `server` las
# perderia en cuanto un location tuviera un add_header propio.

# El panel es interno; nada deberia embeberlo en un iframe.
add_header X-Frame-Options "SAMEORIGIN" always;

# Sin esto, un fichero servido con el tipo equivocado puede ejecutarse como script.
add_header X-Content-Type-Options "nosniff" always;

add_header Referrer-Policy "strict-origin-when-cross-origin" always;

# Politica de contenido ajustada a lo que la aplicacion carga de verdad:
#
#   script-src 'self'          Angular compila AOT: no necesita eval ni scripts
#                              externos. El unico script en linea es el que fija
#                              el tema antes del primer pintado, y va con hash.
#   style-src  'unsafe-inline' Angular inyecta los estilos de cada componente como
#                              <style> en tiempo de ejecucion; sin esto no pinta.
#   fonts.googleapis / gstatic IBM Plex Sans y Mono.
#   connect-src 'self'         Todas las llamadas van a /api, mismo origen.
#   frame-ancestors 'none'     Equivalente moderno de X-Frame-Options.
add_header Content-Security-Policy "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'" always;
