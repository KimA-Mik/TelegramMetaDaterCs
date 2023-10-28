DROP DATABASE "TelegramMetaDater";

CREATE DATABASE "TelegramMetaDater" WITH TEMPLATE = template0 ENCODING = 'UTF8' LOCALE = 'en_US.UTF-8';

CREATE TABLE Supergroups
(
    id            BIGINT PRIMARY KEY,
    title         TEXT,
    main_username TEXT
);


CREATE TABLE Messages
(
    id          SERIAL PRIMARY KEY,
    telegram_id INT,
    sender      BIGINT REFERENCES Supergroups (id),
    content     TEXT
);
CREATE UNIQUE INDEX Messege_TelegramId_Sender ON Messages (telegram_id, sender);

CREATE TABLE Words
(
    id   SERIAL PRIMARY KEY,
    word VARCHAR UNIQUE
);
CREATE UNIQUE INDEX Words_Word ON Words (Word);


CREATE TABLE Words_Messages
(
    id         SERIAL PRIMARY KEY,
    message_id BIGINT REFERENCES Messages (id),
    word_id    INT REFERENCES Words (id),
    count      INT
);
CREATE INDEX WordMessages_WordId ON Words_Messages (word_id);
CREATE UNIQUE INDEX WordMessages_WordId_MessageId ON Words_Messages (Message_Id, word_id);