-- Add EvaluatorLeaderId column to Surveys table
ALTER TABLE "Surveys" ADD COLUMN "EvaluatorLeaderId" integer NULL;

-- Add foreign key constraint
CREATE INDEX "IX_Surveys_EvaluatorLeaderId" ON "Surveys" ("EvaluatorLeaderId");

ALTER TABLE "Surveys"
ADD CONSTRAINT "FK_Surveys_Leaders_EvaluatorLeaderId"
FOREIGN KEY ("EvaluatorLeaderId")
REFERENCES "Leaders" ("Id")
ON DELETE SET NULL;
