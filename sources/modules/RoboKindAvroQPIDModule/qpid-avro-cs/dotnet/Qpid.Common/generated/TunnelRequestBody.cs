
using Apache.Qpid.Buffer;
using System.Text;

namespace Apache.Qpid.Framing
{
  ///
  /// This class is autogenerated
  /// Do not modify.
  ///
  /// @author Code Generator Script by robert.j.greig@jpmorgan.com
  public class TunnelRequestBody : AMQMethodBody , IEncodableAMQDataBlock
  {
    public const int CLASS_ID = 110; 	
    public const int METHOD_ID = 10; 	

    public FieldTable MetaData;    
     

    protected override ushort Clazz
    {
        get
        {
            return 110;
        }
    }
   
    protected override ushort Method
    {
        get
        {
            return 10;
        }
    }

    protected override uint BodySize
    {
    get
    {
        
        return (uint)
        (uint)EncodingUtils.EncodedFieldTableLength(MetaData)		 
        ;
         
    }
    }

    protected override void WriteMethodPayload(ByteBuffer buffer)
    {
        EncodingUtils.WriteFieldTableBytes(buffer, MetaData);
            		 
    }

    protected override void PopulateMethodBodyFromBuffer(ByteBuffer buffer)
    {
        MetaData = EncodingUtils.ReadFieldTable(buffer);
        		 
    }

    public override string ToString()
    {
        StringBuilder buf = new StringBuilder(base.ToString());
        buf.Append(" MetaData: ").Append(MetaData);
         
        return buf.ToString();
    }

    public static AMQFrame CreateAMQFrame(ushort channelId, FieldTable MetaData)
    {
        TunnelRequestBody body = new TunnelRequestBody();
        body.MetaData = MetaData;
        		 
        AMQFrame frame = new AMQFrame();
        frame.Channel = channelId;
        frame.BodyFrame = body;
        return frame;
    }
} 
}
