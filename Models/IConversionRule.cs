namespace ApolloMigration.Models;

public interface IConversionRule
{
    BaseModel Convert(BaseModel source);
    bool CanConvert(BaseModel source);
    string GetTargetDocumentType();
    string GetSourceDocumentType();
}

public interface IConversionRule<TSource, TTarget> : IConversionRule
    where TSource : BaseModel
    where TTarget : BaseModel
{
    new TTarget Convert(TSource source);
    BaseModel IConversionRule.Convert(BaseModel source) => Convert((TSource)source);
}
