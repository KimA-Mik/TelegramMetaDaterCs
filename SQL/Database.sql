DROP DATABASE "TelegramMetaDater";

CREATE DATABASE "TelegramMetaDater" WITH TEMPLATE = template0 ENCODING = 'UTF8' LOCALE = 'en_US.UTF-8';

CREATE TABLE Supergroups (
	id BIGINT PRIMARY KEY,
	title TEXT,
	main_username TEXT
);


CREATE TABLE Messages (
	id BIGINT PRIMARY KEY,
	sender BIGINT REFERENCES Supergroups(id),
	Content TEXT
);


CREATE TABLE Words (
	Id INT PRIMARY KEY,
	Word VARCHAR UNIQUE
);
CREATE UNIQUE INDEX Words_Word ON Words (Word);


CREATE TABLE Word_Messages(
	Id INT PRIMARY KEY,
	Message_Id BIGINT REFERENCES Messages(Id),
	Word_Id INT REFERENCES Words(Id),
	count INT
);
CREATE INDEX WordMessages_WordId ON Word_Messages (Word_Id);
CREATE UNIQUE INDEX WordMessages_WordId_MessageId ON Word_Messages (Message_Id, Word_Id);