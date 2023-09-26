using SunsetNews.Telegram;
using SunsetNews.UserSequences.ReflectionRepository;
using SunsetNews.UserSequences.UserWaitConditions;

namespace SunsetNews;

internal sealed class DemoSequenceModule : ISequenceModule
{
	public DemoSequenceModule()
	{
		
	}


	[UserSequence("hello")]
	public async IAsyncEnumerator<UserWaitCondition> HelloWithBot(IMessage awakeMessage)
	{
		await awakeMessage.Chat.SendMessageAsync(new MessageSendModel("Say something"));

		var w = new TextMessageWaitCondition();
		yield return w;

		await awakeMessage.Chat.SendMessageAsync(new MessageSendModel(w.CapturedMessage.Content));
	}

	[UserSequence("buttons")]
	public async IAsyncEnumerator<UserWaitCondition> ShowButtons(IMessage awakeMessage)
	{
		var message = await awakeMessage.Chat.SendMessageAsync(new MessageSendModel("Say something",
			new MessageButtonGrid(new MessageButton("hi", "Say hello"), new MessageButton("ho", "Say HOLLOY"), new MessageButton("stop", "Exit"))));

		while (true)
		{
			var w = new ButtonWaitCondition(message);
			yield return w;

			switch (w.CapturedButtonId)
			{
				case "hi":
					await awakeMessage.Chat.SendMessageAsync(new MessageSendModel("HELLO my friend"));
					break;

				case "ho":
					await awakeMessage.Chat.SendMessageAsync(new MessageSendModel("Ho, enter your message"));
					var s = new TextMessageWaitCondition();
					yield return s;
					await awakeMessage.Chat.SendMessageAsync(new MessageSendModel($"You said \"{s.CapturedMessage.Content}\""));
					break;

				case "stop":
					await awakeMessage.Chat.SendMessageAsync(new MessageSendModel("Exit ok"));
					yield break;
			}
		}
	}
}
