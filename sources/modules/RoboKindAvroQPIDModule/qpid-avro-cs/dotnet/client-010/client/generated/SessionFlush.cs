/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
 */

using System;
using org.apache.qpid.transport.codec;
using System.Collections.Generic;
using org.apache.qpid.transport.util;
using org.apache.qpid.transport.network;
using System.IO;

namespace org.apache.qpid.transport
{



public sealed class SessionFlush : Method {

    public const int TYPE = 524;

    public override int GetStructType() {
        return TYPE;
    }

    public override int GetSizeWidth() {
        return 0;
    }

    public override int GetPackWidth() {
        return 2;
    }

    public override bool HasPayload() {
        return false;
    }

    public override byte EncodedTrack 
    {
       get{ return Frame.L3; }
       set { throw new NotImplementedException(); }
    }

    private int packing_flags = 0;


    public SessionFlush() {}


    public SessionFlush(params Option[] options) {

        for (int i=0; i < options.Length; i++) {
            switch (options[i]) {
            case Option.EXPECTED: packing_flags |= 256; break;
            case Option.CONFIRMED: packing_flags |= 512; break;
            case Option.COMPLETED: packing_flags |= 1024; break;
            case Option.SYNC: Sync = true; break;
            case Option.BATCH: Batch = true; break;
            case Option.NONE: break;
            default: throw new Exception("invalid option: " + options[i]);
            }
        }

    }

    public override void Dispatch<C>(C context, MethodDelegate<C> mdelegate) {
        mdelegate.SessionFlush(context, this);
    }


    public bool HasExpected() {
        return (packing_flags & 256) != 0;
    }

    public SessionFlush ClearExpected() {
        packing_flags = (byte) (packing_flags & ~256);       

        Dirty = true;
        return this;
    }

    public bool GetExpected() {
        return HasExpected();
    }

    public SessionFlush SetExpected(bool value) {

        packing_flags |=  256;
        Dirty = true;
        return this;
    }


    public bool HasConfirmed() {
        return (packing_flags & 512) != 0;
    }

    public SessionFlush ClearConfirmed() {
        packing_flags = (byte) (packing_flags & ~512);       

        Dirty = true;
        return this;
    }

    public bool GetConfirmed() {
        return HasConfirmed();
    }

    public SessionFlush SetConfirmed(bool value) {

        packing_flags |=  512;
        Dirty = true;
        return this;
    }


    public bool HasCompleted() {
        return (packing_flags & 1024) != 0;
    }

    public SessionFlush ClearCompleted() {
        packing_flags = (byte) (packing_flags & ~1024);       

        Dirty = true;
        return this;
    }

    public bool GetCompleted() {
        return HasCompleted();
    }

    public SessionFlush SetCompleted(bool value) {

        packing_flags |=  1024;
        Dirty = true;
        return this;
    }





    public override void Write(IEncoder enc)
    {
        enc.WriteUint16(packing_flags);

    }

    public override void Read(IDecoder dec)
    {
        packing_flags = (int) dec.ReadUint16();

    }

    public override Dictionary<String,Object> Fields
    {
		get
		{
			Dictionary<String,Object> result = new Dictionary<String,Object>();

        	if ((packing_flags & 256) != 0)
            	result.Add("_Expected", GetExpected());
        	if ((packing_flags & 512) != 0)
            	result.Add("_Confirmed", GetConfirmed());
        	if ((packing_flags & 1024) != 0)
            	result.Add("_Completed", GetCompleted());

			return result;
        }
    }

}
}
