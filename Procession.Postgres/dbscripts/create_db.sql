--This is run by the admin in the default database (the agqueue database will not have existed yet)
DROP OWNED BY agqueue_user;
DROP DATABASE IF EXISTS agqueue;

DROP USER IF EXISTS agqueue_user;

CREATE DATABASE agqueue;

CREATE USER agqueue_user PASSWORD 'everex';
GRANT ALL PRIVILEGES ON DATABASE agqueue to agqueue_user;

SET timezone TO 'America/New_York';




