namespace _Main.Scripts.Ecs.Collection.Cmp
{
	/// <summary>Raised by the scanner UI (through the input service) with the raw decoded QR text.</summary>
	public struct ScanQrCodeReq
	{
		public string RawPayload;
	}
}
