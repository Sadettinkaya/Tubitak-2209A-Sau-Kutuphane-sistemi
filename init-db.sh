#!/bin/bash
set -e

# Wait for database to be fully ready
until pg_isready -h localhost -U postgres; do
  echo "Waiting for database..."
  sleep 2
done

echo "Database is ready. Waiting for migrations to complete..."
sleep 10

# Run identity restore
echo "Restoring identity data..."
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -f /docker-entrypoint-initdb.d/01-restore_identity.sql

# Run reservation/table data restore
echo "Restoring reservation data..."
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -f /docker-entrypoint-initdb.d/02-restore_data.sql

echo "Database initialization completed!"
