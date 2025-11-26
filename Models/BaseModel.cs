using Couchbase.KeyValue;
using Microsoft.Extensions.Configuration.UserSecrets;
using System.Security.Claims;

namespace ApolloMigration.Models;

public abstract class BaseModel
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string DocumentType { get; set; } = string.Empty;
}

public abstract class BookingBase
{
    public uint SegmentId { get; set; }
    public uint SubSegmentId { get; set; }
    public string ExternalOrderId { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public object? Tmura { get; set; }
    public int CcPayments { get; set; }
    public int SalaryPayments { get; set; }
    public List<string> BookingTags { get; set; } = new();
    public uint PeriodEntitledDays { get; set; }
}

public class Booking : BookingBase
{
    public const string WEBSITE = "website";
    public uint Id { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.REQUEST;
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdateTime { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; } = DateTime.Now;
    public List<BookingNote> Notes { get; set; } = new();
    public List<HistoryNote> History { get; set; } = new();
    public string SubsidyComment { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public string GroupId { get; set; } = string.Empty;
    public List<Product> Products { get; set; } = new();
    public List<BookingUser> Users { get; set; } = new();
    public List<Passenger> Passengers = new List<Passenger>();
    public decimal GrossRoomPrice { get; set; }
    public decimal NetRoomPrice { get; set; }
    public FlightDetails? FlightDetails { get; set; }
    public CcTransactionData? CcTransactionData { get; set; }

}

public class HistoryNote
{
    public string EmployeeEmail { get; set; } = string.Empty;
    public DateTime Time { get; set; } = DateTime.Now;
    public List<string> Notes { get; set; } = new();
}

public class BookingUser
{
    public bool Confirmed { get; set; }
    public string Key { get; set; } = string.Empty;
    public List<string> SpecialRequests { get; set; } = new();
    public object ClientPrice { get; set; }
    public decimal BasePrice { get; set; }
    public PaymentType PaymentType { get; set; } = PaymentType.Salary;
    public decimal ClientTotal { get; set; }
    public List<string> Requests { get; set; } = new();
    public Dictionary<string, string[]> RequestSections { get; set; } = new();

    public List<Activities> Activities { get; set; }
}

public class Passenger
{
    public string Key { get; set; } = string.Empty;
    public string UserKey { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string PassportNo { get; set; } = string.Empty;
    public DateOnly PassportExp { get; set; }

    public List<Activities> Activities { get; set; }
}


public class BookingNote
{
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; } = DateTime.Now;
    public string Note { get; set; } = string.Empty;
    public bool IsOperations { get; set; }
    public bool IsSalesJunior { get; set; }
    public bool IsSalesSenior { get; set; }
    public string Id { get; set; } = string.Empty;

}


public class FlightDetails
{
    public FlightArrivalDetails Arrival { get; set; } = new();
    public FlightDepartureDetails Departure { get; set; } = new();
}

public class FlightArrivalDetails
{
    public string FlightDate { get; set; } = string.Empty;
    public string FlightNumber { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string LandingTime { get; set; } = string.Empty;
}

public class FlightDepartureDetails
{
    public string FlightDate { get; set; } = string.Empty;
    public string FlightNumber { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string TakeoffTime { get; set; } = string.Empty;
}

public class CcTransactionData
{
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentType PaymentType { get; set; } = PaymentType.TodayCreditCard;

}

public class Product
{
    public string Name { get; set; } = string.Empty;
    public BookingStatus Status { get; set; } = BookingStatus.REQUEST;
    public IProductDetails? ProductDetails { get; set; }
    public CancellationDetails? CancellationDetails { get; set; }
    public decimal NetAdjustment { get; set; } = 0;
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdateTime { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; } = DateTime.Now;    
}

public interface IProductDetails
{
    public ProductTypeEnum Type { get; set; }
}

// Product detail classes
public class HotelDetails : IProductDetails
{
    public ProductTypeEnum Type { get; set; } = ProductTypeEnum.Hotel;
    public bool Apollo { get; set; }
    public uint AtlantisHotelId { get; set; }
    public string BookingIdFromHotel { get; set; } = string.Empty;
    public uint HotelSegmentId { get; set; }
    public List<object> Rooms { get; set; } = new();
    public DateOnly Start { get; set; } = new();
    public DateOnly End { get; set; }
    public object Pax { get; set; }
    public string RoomLabel { get; set; } = string.Empty;
}

public class InterestDetails : IProductDetails
{
    public ProductTypeEnum Type { get; set; } = ProductTypeEnum.Interest;
    public decimal Interest { get; set; } = 0;
}

public class Activities : IProductDetails
{
    public ProductTypeEnum Type { get; set; } = ProductTypeEnum.Activity;
    public string Activity { get; set; } = string.Empty;
    public string Option { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string UserKey { get; set; } = string.Empty;
    public string PassengerKey { get; set; } = string.Empty;
}

public class CancellationDetails
{
    public decimal? CancellationFeeNet { get; set; } = 0;
    public decimal? CancellationFeeGross { get; set; } = 0;
    public DateTime CancelledTime { get; set; } = DateTime.Now;
    public string CancellationReason { get; set; } = string.Empty;
}

public class FitPaxComposition
{
    public uint SmallChildren { get; set; }
    public uint Adults { get; set; }
    public uint Infants { get; set; }

    public virtual uint TotalPaxWithoutInfants()
    {
        return Adults + SmallChildren;
    }
    public virtual uint TotalPax()
    {
        return Adults + SmallChildren + Infants;
    }
}

public class RoomPaxComposition : FitPaxComposition
{
    public uint? BigChildren { get; set; }
    public RoomPaxComposition() { }
    public RoomPaxComposition(RoomPaxComposition other)
    {

        Adults = other.Adults;
        BigChildren = other.BigChildren;
        SmallChildren = other.SmallChildren;
        Infants = other.Infants;
    }
    public override string ToString()
    {
        return $"Adults: {Adults}, Big: {BigChildren}, Small: {SmallChildren}, Infants: {Infants}";
    }

    public string ToNumbersString()
    {
        var parts = new List<uint>();

        if (Adults != 0) parts.Add(Adults);
        if (BigChildren.HasValue && BigChildren.Value != 0) parts.Add(BigChildren.Value);
        if (SmallChildren != 0) parts.Add(SmallChildren);
        if (Infants != 0) parts.Add(Infants);

        return string.Join(", ", parts);
    }



    public override uint TotalPaxWithoutInfants()
    {
        return Adults + SmallChildren + (BigChildren ?? 0);
    }
    public override uint TotalPax()
    {
        return Adults + SmallChildren + (BigChildren ?? 0) + Infants;
    }

    public uint AdultsAndBig()
    {
        return Adults + (BigChildren ?? 0);
    }

    public uint TotalChildren()
    {
        return SmallChildren + (BigChildren ?? 0);
    }
}


    public enum BookingStatus
{
    UNKOWN,
    REQUEST,
    CANCELLED,
    OK,
    OKR,
}

public enum PaymentType
{
    Salary,
    TodayCreditCard,
    FuturePayment,
    FutureCreditCard,
    NoPayment
}

public enum ProductTypeEnum
{
    Hotel,
    Service,
    Interest,
    Billing,
    Activity
}

