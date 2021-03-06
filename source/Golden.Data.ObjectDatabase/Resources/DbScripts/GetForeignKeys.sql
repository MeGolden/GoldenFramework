﻿SELECT 
	fkcol.TABLE_SCHEMA AS FKSchema,
	fkcol.TABLE_NAME AS FKTable,
	fkcol.COLUMN_NAME AS FKColumn,
	pkcol.TABLE_SCHEMA AS PKSchema,
	pkcol.TABLE_NAME AS PKTable,
	pkcol.COLUMN_NAME AS PKColumn,
	rc.DELETE_RULE AS DeleteRule,
	rc.UPDATE_RULE AS UpdateRule,
	rc.CONSTRAINT_NAME AS [Name]
FROM 
	INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
	INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS pkcol ON 
		rc.UNIQUE_CONSTRAINT_SCHEMA = pkcol.CONSTRAINT_SCHEMA
		AND rc.UNIQUE_CONSTRAINT_NAME = pkcol.CONSTRAINT_NAME
	INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS fkcol ON 
		rc.CONSTRAINT_SCHEMA = fkcol.CONSTRAINT_SCHEMA 
		AND rc.CONSTRAINT_NAME = fkcol.CONSTRAINT_NAME
WHERE 
	ISNULL(OBJECTPROPERTY(OBJECT_ID(rc.CONSTRAINT_NAME), 'IsMSShipped'), 0) = 0
	AND pkcol.ORDINAL_POSITION = fkcol.ORDINAL_POSITION
	AND fkcol.TABLE_SCHEMA = @Schema AND fkcol.TABLE_NAME = @Name