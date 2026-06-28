CREATE TABLE IF NOT EXISTS submissions (
    id TEXT PRIMARY KEY,
    timestamp TEXT NOT NULL,
    device_metadata TEXT,
    answers_json TEXT
);
