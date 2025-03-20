using Console.AI1.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Console.AI1.Demo.SKMultiAgentChat.Plugins;
public class AgentAItemsPlugin(IConfiguration config)
{
    [KernelFunction("SearchItems")]
    [Description("Retrieves items from the database")]
    [return: Description("List of items")]
    public static List<Item> SearchItems()
    {
        return [
                new() {
                    Name = "CustomData1A", Description = "Animal", StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(20,0,0), EnumValue = CommonEnum.Value1,
                    EnumList = [CommonEnum.Value3, CommonEnum.Value4]
                },
                new() {
                    Name = "CustomData2A", Description = "Vegetable", StartTime = new TimeSpan(2, 0, 0), EndTime = new TimeSpan(12,0,0), EnumValue = CommonEnum.Value1,
                    EnumList = [CommonEnum.Value1, CommonEnum.Value3]
                },
                new() {
                    Name = "CustomData3A", Description = "Mineral", StartTime = new TimeSpan(1, 0, 0), EndTime = new TimeSpan(15,0,0), EnumValue = CommonEnum.Value1,
                    EnumList = [CommonEnum.Value2, CommonEnum.Value3]
                }
            ];
    }
}
