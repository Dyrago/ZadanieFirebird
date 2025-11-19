-- domena dla identyfikatora
CREATE DOMAIN DM_ID AS INTEGER NOT NULL;

-- domena dla nazw użytkowników
CREATE DOMAIN DM_USERNAME AS VARCHAR(50) NOT NULL;

-- domena dla daty i czasu
CREATE DOMAIN DM_DATETIME AS TIMESTAMP;
