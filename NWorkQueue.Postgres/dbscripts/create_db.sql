--This is run by the admin in the default database (the nworkqueue database will not have existed yet)
DROP OWNED BY nworkqueue_user;
DROP DATABASE IF EXISTS nworkqueue;

DROP USER IF EXISTS nworkqueue_user;

CREATE DATABASE nworkqueue;

CREATE USER nworkqueue_user PASSWORD 'everex';
GRANT ALL PRIVILEGES ON DATABASE nworkqueue to nworkqueue_user;

SET timezone TO 'America/New_York';




