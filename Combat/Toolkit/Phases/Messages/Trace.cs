namespace Combat.Toolkit
{
	public static class Trace
	{
		public struct Splitter
		{
			public Splitter(string text)
			{
				this.text = text;
			}

			public string text;
		}

		public struct Log
		{
			public Log(string text)
			{
				this.text = text;
			}

			public string text;
		}

		public struct Warning
		{
			public string text;

			public Warning(string text)
			{
				this.text = text;
			}
		}

		public struct Error
		{
			public Error(string text)
			{
				this.text = text;
			}

			public string text;
		}
	}
}