@ignore
@TodoItemCRUD

#[Microsoft.VisualStudio.TestTools.UnitTesting.DoNotParallelize()] attribute in feature.cs
@mstest:donotparallelize

Feature: TodoItem Add, edit, delete

#Run the api in another VS
#This scenario does not currently search or page through items, so less than 10 in the DB required to show on the first page

Scenario: TodoItem add edit delete
	#Given some name-value pairs as params sent in to the step definition methods
	#| name	  | value |
	#| name1  | val1  |
	#| name2  | val2  |
	Given the user navigates to the main page
	When user enters <todoItemName> in textbox and clicks Add
	Then verify the item exists in the grid
	When user clicks the edit button for this item
	Then verify the edit area shows the item
	When user appends the name with <appendsName> and clicks Update
	Then verify the item exists in the grid
	When user clicks the edit button for this item
	Then verify the edit area shows the item
	When user checks the complete box and clicks Update
	Then verify the item complete box is checked in the grid
	When user clicks the delete button for this item
	Then verify the item is no longer in the grid

Examples:
	| todoItemName | appendsName |
	| item1a       | 123         |
	| item2a       | 321         |
