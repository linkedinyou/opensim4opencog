// ------------------------------------------------------------------------------
// <auto-generated>
//    Generated by RoboKindChat.vshost.exe, version 0.9.0.0
//    Changes to this file may cause incorrect behavior and will be lost if code
//    is regenerated
// </auto-generated>
// ------------------------------------------------------------------------------
namespace org.robokind.avrogen.animation
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Avro;
	using Avro.Specific;
	
	public interface PlayRequest
	{
		Schema Schema
		{
			get;
		}
		string sourceId
		{
			get;
		}
		string destinationId
		{
			get;
		}
		long currentTimeMillisec
		{
			get;
		}
		string animationName
		{
			get;
		}
		string animationVersionNumber
		{
			get;
		}
	}
}
