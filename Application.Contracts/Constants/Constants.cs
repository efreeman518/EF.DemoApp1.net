﻿namespace Application.Contracts.Constants;

public static class Constants
{
    //Error messages
    public const string ERROR_NAME_EXISTS = "Item name '{0}' already exists";
    public const string ERROR_ITEM_NOTFOUND = "Item not found";
    public const string ERROR_URL_BODY_ID_MISMATCH = "Url Id does not match payload Id";
    public const string ERROR_RULE_NAME_LENGTH_MESSAGE = "Name length must be bewteen {0} and {1}.";
    public const string ERROR_RULE_NAME_INVALID_MESSAGE = "Name is invalid; required pattern: '{0}'";
    public const string ERROR_RULE_INVALID_MESSAGE = "Item is invalid";
}
