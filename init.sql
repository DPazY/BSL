CREATE TABLE IF NOT EXISTS editions (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL
);

CREATE TABLE IF NOT EXISTS books (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    author VARCHAR(255) NOT NULL,
    publisher VARCHAR(255) NOT NULL,
    year INT NOT NULL,
    pages_count INT NOT NULL
);

CREATE TABLE IF NOT EXISTS patents (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    author VARCHAR(255) NOT NULL,
    country VARCHAR(100) NOT NULL,
    number INT NOT NULL
);

CREATE TABLE IF NOT EXISTS newspapers (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    issue_number INT NOT NULL,
    place_of_publication VARCHAR(255) NOT NULL,
    city VARCHAR(100) NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_books_name ON books(name);
CREATE INDEX IF NOT EXISTS ix_editions_name ON editions(name);