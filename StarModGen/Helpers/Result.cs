namespace StarModGen.Helpers
{
	public readonly struct Result<TR, TE>
		where TR : struct
		where TE : struct
	{
		public readonly bool OK;
		public readonly TR Value = default;
		public readonly TE Error = default;

		public Result(TR Result)
		{
			OK = true;
			Value = Result;
		}

		public Result(TE Error)
		{
			OK = false;
			this.Error = Error;
		}
	}
}
