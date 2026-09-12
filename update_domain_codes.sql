UPDATE "BankTopics" 
SET "DomainCode" = 'BANKING',
    "MaterializedPath" = '/BANKING/2026_DOT2/'
WHERE "Code" = '2026_DOT2';

UPDATE "BankTopics" 
SET "DomainCode" = 'BANKING',
    "MaterializedPath" = '/BANKING/2026_DOT2/' || "Code" || '/'
WHERE "ParentId" = (SELECT "Id" FROM "BankTopics" WHERE "Code" = '2026_DOT2');

UPDATE "BankTopics" 
SET "DomainCode" = 'EDUCATION',
    "MaterializedPath" = '/EDUCATION/TNPT_TIENG_ANH/'
WHERE "Code" = 'TNPT_TIENG_ANH';
