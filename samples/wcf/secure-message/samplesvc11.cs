using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Security;
using System.ServiceModel.Security.Tokens;
using System.Xml;

public class Test
{
	class MyEncoder : SecurityStateEncoder
	{
		protected override byte [] DecodeSecurityState (byte [] src)
		{
foreach (byte b in src) Console.Write ("{0:X02} ", b); Console.WriteLine ();
			Console.WriteLine ("Decoding ...");
			DumpXmlBinary (src);
			return src;
		}
		protected override byte [] EncodeSecurityState (byte [] src)
		{
foreach (byte b in src) Console.Write ("{0:X02} ", b); Console.WriteLine ();
			// this show how it is LAMESPEC.
			//Array.Reverse (src);
			Console.WriteLine ("Encoding ...");
			DumpXmlBinary (src);
			return src;
		}

		void DumpXmlBinary (byte [] src)
		{
XmlDictionary dic = new XmlDictionary ();
for (int i = 0; i < 12; i++)
	dic.Add ("n" + i);
XmlDictionaryReaderQuotas quotas =
	new XmlDictionaryReaderQuotas ();
XmlDictionaryReader cr = XmlDictionaryReader.CreateBinaryReader (src, 0, src.Length, dic, quotas);
cr.Read ();
XmlWriter w = XmlWriter.Create (Console.Out);
while (!cr.EOF)
	w.WriteNode (cr, false);
w.Close ();
Console.WriteLine ();
		}
	}

	public static void Main ()
	{
		SymmetricSecurityBindingElement sbe =
			new SymmetricSecurityBindingElement ();
		sbe.ProtectionTokenParameters =
			new SslSecurityTokenParameters (true);
		ServiceHost host = new ServiceHost (typeof (Foo));
		HttpTransportBindingElement hbe =
			new HttpTransportBindingElement ();
		CustomBinding binding = new CustomBinding (sbe, hbe);
		binding.ReceiveTimeout = TimeSpan.FromSeconds (5);
		host.AddServiceEndpoint ("IFoo",
			binding, new Uri ("http://localhost:8080"));
		ServiceCredentials cred = new ServiceCredentials ();
		//cred.SecureConversationAuthentication.SecurityStateEncoder =
		//	new MyEncoder ();
		cred.ServiceCertificate.Certificate =
			new X509Certificate2 ("test.pfx", "mono");
		cred.ClientCertificate.Authentication.CertificateValidationMode =
			X509CertificateValidationMode.None;
		host.Description.Behaviors.Add (cred);
		host.Description.Behaviors.Find<ServiceDebugBehavior> ()
			.IncludeExceptionDetailInFaults = true;
//		foreach (ServiceEndpoint se in host.Description.Endpoints)
//			se.Behaviors.Add (new StdErrInspectionBehavior ());
		ServiceMetadataBehavior smb = new ServiceMetadataBehavior ();
		smb.HttpGetEnabled = true;
		smb.HttpGetUrl = new Uri ("http://localhost:8080/wsdl");
		host.Description.Behaviors.Add (smb);
		host.Open ();
		Console.WriteLine ("Hit [CR] key to close ...");
		Console.ReadLine ();
		host.Close ();
	}
}

[ServiceContract]// (SessionMode = SessionMode.NotAllowed)]
public interface IFoo
{
	[OperationContract]
	string Echo (string msg);
}

[ServiceBehavior (IncludeExceptionDetailInFaults = true)]
class Foo : IFoo
{
	public string Echo (string msg) 
	{
XmlWriterSettings xws = new XmlWriterSettings ();
xws.Indent = true;
using (XmlWriter xw = XmlWriter.Create (Console.Out, xws)) {
	xw.WriteStartElement ("root");
	MessageHeaders hs = OperationContext.Current.IncomingMessageHeaders;
	for (int i = 0; i < hs.Count; i++) hs.WriteHeader (i, xw);
}
Console.WriteLine (msg);
		return msg + msg;
		//throw new NotImplementedException ();
	}
}

