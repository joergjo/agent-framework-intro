using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentOtel;

public class UserInfo
{
    public string? AdditionalInstructions { get; set; }
}

public sealed class UserInfoMemory(IChatClient chatClient, Func<AgentSession?, UserInfo>? stateInitializer = null)
    : AIContextProvider
{
    private readonly ProviderSessionState<UserInfo> _sessionState = new(
        stateInitializer ?? (_ => new UserInfo()), nameof(UserInfoMemory));

    public override IReadOnlyList<string> StateKeys => field ??= [_sessionState.StateKey];

    public UserInfo GetUserInfo(AgentSession session) => _sessionState.GetOrInitializeState(session);

    public void SetUserInfo(AgentSession session, UserInfo userInfo) => _sessionState.SaveState(session, userInfo);

    protected override async ValueTask StoreAIContextAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context.Session);
        var userInfo = GetUserInfo(context.Session);

        if (userInfo.AdditionalInstructions is null || context.RequestMessages.Any(x => x.Role == ChatRole.User))
        {
            var result = await chatClient.GetResponseAsync<UserInfo>(
                context.RequestMessages,
                new ChatOptions
                {
                    Instructions =
                        "Extract any additional instructions from the user's message if present. If not present return null."
                },
                cancellationToken: cancellationToken);
            userInfo.AdditionalInstructions ??= result.Result.AdditionalInstructions;
        }
        
        SetUserInfo(context.Session, userInfo);
    }

    protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context.Session);
        var userInfo = GetUserInfo(context.Session);
        return new ValueTask<AIContext>(
            new AIContext
            {
                Instructions = userInfo?.AdditionalInstructions
            });
    }
}