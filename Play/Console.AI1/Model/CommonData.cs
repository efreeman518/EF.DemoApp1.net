using Microsoft.Extensions.VectorData;

namespace Console.AI1.Model;
public class Item
{
    [VectorStoreKey]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [VectorStoreData]
    public string Name { get; set; } = null!;

    [VectorStoreData]
    public string Description { get; set; } = null!;

    [VectorStoreData]
    public TimeSpan StartTime { get; set; }

    [VectorStoreData]
    public TimeSpan EndTime { get; set; }

    [VectorStoreData]
    public CommonEnum EnumValue { get; set; }

    [VectorStoreData]
    public List<CommonEnum> EnumList { get; set; } = [];

    [VectorStoreVector(Dimensions: 384, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; set; }

}
