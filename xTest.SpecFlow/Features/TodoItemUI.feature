Feature: TodoItemUI
	Add, edit, remove TodoItems

#Run the api in another VS
#The versions of Chrome and Edge must match the versions of the drivers in the bin folder
#This scenario does not currently search or page through items, so less than 10 in the DB required to show on the first page
@ignore
@tag1

Scenario: TodoItem CRUD
	#Given some name-value pairs as params sent in to the step definition methods
	#| name	  | value |
	#| name1  | val1  |
	#| name2  | val2  |
	Given the client configuration <browser>
	Given user browser navigates to https://localhost:44318/
	When enters <todoItemName> in textbox and clicks Add
	Then verify the item exists in the list
	When user clicks the edit button for this item
	Then verify the edit area shows the item
	When user appends the name with <appendsName> and clicks save
	Then verify the item exists in the list
	When user clicks the edit button for this item
	Then verify the edit area shows the item
	When user checks the complete box and clicks save
	Then verify the item complete box is checked in the list
	When user clicks the delete button for this item
	Then verify the item is no longer in the list

Examples:
	| browser | todoItemName | appendsName |
	| Chrome  | item1a       | 123         |
	| Edge    | item2a       | 321         |



