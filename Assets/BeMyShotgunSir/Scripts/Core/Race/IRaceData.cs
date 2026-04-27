using BeMyShotgunSir.Core;

namespace BeMyShotgunSir.Scripts.Core.Race
{
    public interface IRaceNetData : INetData
    {

    }

    public interface IRaceData : IData
    {

    }

    public interface IRaceDataView : IRaceData, IRaceNetData
    {
        // Define events for data changes if needed
    }
}
