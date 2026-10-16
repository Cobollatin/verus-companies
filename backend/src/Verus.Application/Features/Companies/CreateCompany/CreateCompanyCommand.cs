using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.Logging;
using Verus.Application.Abstractions;
using Verus.Application.Common.Relevance;
using Verus.Application.Common.Validation;
using Verus.Domain.Companies;
using Verus.Domain.Relevance;

namespace Verus.Application.Features.Companies.CreateCompany;

public sealed record CreateCompanyCommand(string? Name, string? WebsiteUrl) : ICommand<ErrorOr<CompanyResponse>>;

public sealed class CreateCompanyValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyValidator()
    {
        RuleFor(c => c.Name).MustBeValueObject(CompanyName.Create);
        RuleFor(c => c.WebsiteUrl).MustBeValueObject(WebsiteUrl.Create);
    }
}

public sealed partial class CreateCompanyHandler(
    ICompanyRepository repository,
    IRelevanceEvaluator relevanceEvaluator,
    TimeProvider clock,
    ILogger<CreateCompanyHandler> logger
) : ICommandHandler<CreateCompanyCommand, ErrorOr<CompanyResponse>>
{
    public async ValueTask<ErrorOr<CompanyResponse>> Handle(
        CreateCompanyCommand command,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        // Input was validated by the pipeline; the value objects still guard themselves.
        var name = CompanyName.Create(command.Name);
        var website = WebsiteUrl.Create(command.WebsiteUrl);
        if (name.IsError || website.IsError)
        {
            return name.ErrorsOrEmptyList.Concat(website.ErrorsOrEmptyList).ToList();
        }

        if (await repository.ExistsWithRegistrableDomainAsync(website.Value.RegistrableDomain, cancellationToken))
        {
            return CompanyErrors.DuplicateDomain(website.Value.RegistrableDomain);
        }

        var relevance = await relevanceEvaluator.EvaluateAsync(name.Value, website.Value, cancellationToken);

        var company = Company.Create(name.Value, website.Value, relevance, clock);
        if (company.IsError)
        {
            LogRejected(logger, name.Value.Value, website.Value.Host, relevance.Score, relevance.Reason);
            return company.Errors;
        }

        await repository.AddAsync(company.Value, cancellationToken);
        LogCreated(logger, company.Value.Id.Value, name.Value.Value, relevance.Method, relevance.Score);

        return company.Value.ToResponse();
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Company {CompanyId} '{Name}' created (relevance {Method} {Score:F2})"
    )]
    private static partial void LogCreated(
        ILogger logger,
        Guid companyId,
        string name,
        RelevanceMethod method,
        double score
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Company '{Name}' rejected for {Host}: relevance {Score:F2}. {Reason}"
    )]
    private static partial void LogRejected(ILogger logger, string name, string host, double score, string reason);
}
