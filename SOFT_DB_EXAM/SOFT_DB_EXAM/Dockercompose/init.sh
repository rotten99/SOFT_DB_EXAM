# init.sh
#!/bin/sh
set -e

# 1) Let all mongod & mongos services start
sleep 20

# 2) Bootstrap the sharded cluster
mongosh --quiet --host mongos:27017 /docker-entrypoint-initdb.d/combined-init.js

# 3) Check if Movies collection has documents
DOC_COUNT=$(
  mongosh --quiet --host mongos:27017 \
    --eval "db.getSiblingDB('MoviesDatabase').Movies.countDocuments()"
)

if [ "$DOC_COUNT" -eq "0" ]; then
  echo "Seeding Movies collection from CSV..."
  mongoimport \
    --host mongos:27017 \
    --db MoviesDatabase \
    --collection Movies \
    --type csv \
    --headerline \
    --file /docker-entrypoint-initdb.d/movies.csv
else
  echo "Movies collection already has $DOC_COUNT documents; skipping import."
fi
