CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "Leaders" (
    "Id" serial NOT NULL,
    "Name" text NOT NULL,
    "Area" text NOT NULL,
    CONSTRAINT "PK_Leaders" PRIMARY KEY ("Id")
);

CREATE TABLE "QuestionCategories" (
    "Id" serial NOT NULL,
    "Name" text NOT NULL,
    "Description" text NOT NULL,
    CONSTRAINT "PK_QuestionCategories" PRIMARY KEY ("Id")
);

CREATE TABLE "Surveys" (
    "Id" serial NOT NULL,
    "Name" text NOT NULL,
    "Description" text NOT NULL,
    "Area" text NOT NULL,
    "LeaderId" integer,
    "EvaluatorLeaderId" integer,
    "Date" timestamp with time zone NOT NULL,
    "MonthYear" timestamp with time zone,
    "CompletedDate" timestamp with time zone,
    "Status" text NOT NULL,
    CONSTRAINT "PK_Surveys" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Surveys_Leaders_EvaluatorLeaderId" FOREIGN KEY ("EvaluatorLeaderId") REFERENCES "Leaders" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_Surveys_Leaders_LeaderId" FOREIGN KEY ("LeaderId") REFERENCES "Leaders" ("Id") ON DELETE SET NULL
);

CREATE TABLE "Questions" (
    "Id" serial NOT NULL,
    "Text" text NOT NULL,
    "QuestionType" text NOT NULL,
    "QuestionOrder" integer NOT NULL,
    "SurveyId" integer NOT NULL,
    CONSTRAINT "PK_Questions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Questions_Surveys_SurveyId" FOREIGN KEY ("SurveyId") REFERENCES "Surveys" ("Id") ON DELETE CASCADE
);

CREATE TABLE "SurveyResponses" (
    "Id" serial NOT NULL,
    "SurveyId" integer NOT NULL,
    "LeaderId" integer NOT NULL,
    "CompletionDate" timestamp with time zone NOT NULL,
    "AdditionalNotes" text,
    CONSTRAINT "PK_SurveyResponses" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SurveyResponses_Leaders_LeaderId" FOREIGN KEY ("LeaderId") REFERENCES "Leaders" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SurveyResponses_Surveys_SurveyId" FOREIGN KEY ("SurveyId") REFERENCES "Surveys" ("Id") ON DELETE CASCADE
);

CREATE TABLE "QuestionCategoryMappings" (
    "Id" serial NOT NULL,
    "QuestionId" integer NOT NULL,
    "CategoryId" integer NOT NULL,
    CONSTRAINT "PK_QuestionCategoryMappings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_QuestionCategoryMappings_QuestionCategories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "QuestionCategories" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_QuestionCategoryMappings_Questions_QuestionId" FOREIGN KEY ("QuestionId") REFERENCES "Questions" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Answers" (
    "Id" serial NOT NULL,
    "QuestionId" integer NOT NULL,
    "SurveyResponseId" integer NOT NULL,
    "Response" text NOT NULL,
    CONSTRAINT "PK_Answers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Answers_Questions_QuestionId" FOREIGN KEY ("QuestionId") REFERENCES "Questions" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Answers_SurveyResponses_SurveyResponseId" FOREIGN KEY ("SurveyResponseId") REFERENCES "SurveyResponses" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Answers_QuestionId" ON "Answers" ("QuestionId");

CREATE INDEX "IX_Answers_SurveyResponseId" ON "Answers" ("SurveyResponseId");

CREATE INDEX "IX_QuestionCategoryMappings_CategoryId" ON "QuestionCategoryMappings" ("CategoryId");

CREATE INDEX "IX_QuestionCategoryMappings_QuestionId" ON "QuestionCategoryMappings" ("QuestionId");

CREATE INDEX "IX_Questions_SurveyId" ON "Questions" ("SurveyId");

CREATE INDEX "IX_SurveyResponses_LeaderId" ON "SurveyResponses" ("LeaderId");

CREATE INDEX "IX_SurveyResponses_SurveyId" ON "SurveyResponses" ("SurveyId");

CREATE INDEX "IX_Surveys_EvaluatorLeaderId" ON "Surveys" ("EvaluatorLeaderId");

CREATE INDEX "IX_Surveys_LeaderId" ON "Surveys" ("LeaderId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250702011302_InitialCreate', '9.0.2');

COMMIT;

