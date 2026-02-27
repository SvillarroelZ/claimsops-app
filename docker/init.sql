-- Initialize PostgreSQL database
-- Create superuser 'postgres' if it doesn't exist
CREATE ROLE postgres SUPERUSER CREATEDB CREATEROLE LOGIN PASSWORD 'postgres';
