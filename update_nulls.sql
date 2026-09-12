UPDATE "BankTopics" SET "MaterializedPath" = '/' || COALESCE("DomainCode", 'GENERAL') || '/' || "Code" || '/' WHERE "MaterializedPath" IS NULL;
UPDATE "BankTopics" SET "Urn" = 'urn:aegis:topic:' || lower("Code") WHERE "Urn" IS NULL;
