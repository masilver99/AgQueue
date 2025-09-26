docker cp ./create_tables.sql posgres12:/tmp/create_tables.sql
docker cp ./create_db.sql posgres12:/tmp/create_db.sql

docker exec -u postgres posgres12 psql -f /tmp/create_db.sql
docker exec -u postgres posgres12 psql -d agqueue -f /tmp/create_tables.sql


