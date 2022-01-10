using System;
using Microsoft.Extensions.Configuration;

public class ExternalServicesConfiguration : IExternalServicesConfiguration
{
  private IConfiguration _externalServicesConfigSection;

  public ExternalServicesConfiguration(IConfiguration configuration)
  {
    if (configuration == null)
    {
      throw new ArgumentNullException(nameof(configuration));
    }

    _externalServicesConfigSection = configuration.GetSection("ExternalServices");
  }

  public string SlackDownloadUrl => _externalServicesConfigSection["Slack:DownloadUrl"] ?? throw new MemberAccessException("Slack Download Url is missing in appsetting.json file");
  public string SlackInvitationUrl => _externalServicesConfigSection["Slack:InvitationUrl"] ?? throw new MemberAccessException("Slack Invitation Url is missing in appsetting.json file");
  public string UserManualUrl => _externalServicesConfigSection["UserManualUrl"] ?? throw new MemberAccessException("User Manual Url is missing in appsetting.json file");
  public string FacebookGroupUrl => _externalServicesConfigSection["FacebookGroupUrl"] ?? throw new MemberAccessException("Facebook Group Url is missing in appsetting.json file");
}