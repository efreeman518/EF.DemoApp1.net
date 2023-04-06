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
| 5   | asdfrx            | true   |
| 10  | asdfrx            | false  |
| 12  | adsfxjtyfhshgfhy  | true   |
| 7   | atr3t             | false  |
