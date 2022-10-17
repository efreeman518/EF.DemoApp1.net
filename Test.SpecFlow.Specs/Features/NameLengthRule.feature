Feature: NameLengthRule
	Validates TodoItem using TodoItemNameLengthRule

@mytag
Scenario: Validate TodoItem Name Length
	Given the Name length requirement is <len>
	And the TodoItem Name is <name>
	When the TodoItem Name is validated
	Then the valid result should be <result>

Examples: 
| len | name              | result |
| 5   | asdfrt            | true   |
| 10  | asdfrt            | false  |
| 12  | sdsfggftyfhshgfhy | true   |
| 3   | tr                | false  |
