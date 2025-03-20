using Console.AI1.Model;
using Microsoft.Extensions.Configuration;

namespace Console.AI1.Demo.MSExtAI.Plugins;

public class DataPluginB(IConfiguration config)
{
    public List<Item> SearchItems()
    {
        return [
                new() {
                    Name = "CustomData1B", Description = "Animal", StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(22,0,0), EnumValue = CommonEnum.Value1,
                    EnumList = [CommonEnum.Value1, CommonEnum.Value2, CommonEnum.Value3]
                },
                new() {
                    Name = "CustomData2B", Description = "Vegetable", StartTime = new TimeSpan(4, 0, 0), EndTime = new TimeSpan(8,0,0), EnumValue = CommonEnum.Value1,
                    EnumList = [CommonEnum.Value1]
                },
                new() {
                    Name = "CustomData3B", Description = "Mineral", StartTime = new TimeSpan(2, 0, 0), EndTime = new TimeSpan(9,0,0), EnumValue = CommonEnum.Value1,
                    EnumList = [CommonEnum.Value5]
                }
            ];
    }
}
