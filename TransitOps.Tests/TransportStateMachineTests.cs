using TransitOps.Api.Domain.Entities;
using TransitOps.Api.Domain.Enums;

namespace TransitOps.Tests;

public sealed class TransportStateMachineTests
{
    [Theory]
    [InlineData(TransportStatus.Planned, TransportStatus.InTransit, true)]
    [InlineData(TransportStatus.Planned, TransportStatus.Cancelled, true)]
    [InlineData(TransportStatus.Planned, TransportStatus.Delivered, false)]
    [InlineData(TransportStatus.InTransit, TransportStatus.Delivered, true)]
    [InlineData(TransportStatus.InTransit, TransportStatus.Cancelled, true)]
    [InlineData(TransportStatus.Delivered, TransportStatus.Planned, false)]
    [InlineData(TransportStatus.Cancelled, TransportStatus.InTransit, false)]
    public void CanTransitionTo_ReturnsExpectedResult(
        TransportStatus currentStatus,
        TransportStatus targetStatus,
        bool expectedResult)
    {
        var transport = new Transport(currentStatus);

        var result = transport.CanTransitionTo(targetStatus);

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void TransitionTo_Throws_WhenTransitionIsInvalid()
    {
        var transport = new Transport(TransportStatus.Delivered);

        var action = () => transport.TransitionTo(TransportStatus.Planned);

        Assert.Throws<InvalidOperationException>(action);
    }
}
