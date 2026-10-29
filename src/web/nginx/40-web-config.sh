#!/bin/sh
set -eu

cat > /usr/share/nginx/html/config.json <<EOF
{
  "archiveUrl": "${ARCHIVE_URL}",
  "exportUrl": "${EXPORT_URL}"
}
EOF
