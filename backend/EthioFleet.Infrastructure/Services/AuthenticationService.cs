private readonly IEmailSender _emailSender;

public AuthenticationService(IEmailSender emailSender,
                             /* other deps */)
{
    _emailSender = emailSender;
    // existing assignments …
}
if (result.Succeeded)
{
    // Build a simple welcome/confirmation message
    var subject = "Welcome to EthioFleet – Confirm Your Email";
    var html = $@"
        <h2>Welcome, {request.FirstName ?? "User"}!</h2>
        <p>Thanks for registering. Please check your inbox for a confirmation link.</p>";

    await _emailSender.SendAsync(request.Email,
                                 request.FirstName ?? request.Email,
                                 subject,
                                 html);
}