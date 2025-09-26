--This is run by the admin in the default database (the procession database will not have existed yet)
DROP OWNED BY procession_user;
DROP DATABASE IF EXISTS procession;

DROP USER IF EXISTS procession_user;

CREATE DATABASE procession;

CREATE USER procession_user PASSWORD 'everex';
GRANT ALL PRIVILEGES ON DATABASE procession to procession_user;

SET timezone TO 'America/New_York';




