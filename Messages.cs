using System;
using System.Text;

namespace TCPServerV2
{
    public static class Messages
    {
        //Server messages types
        public const string msgTurnLeft = "103 TURN LEFT";
        public const string msgMove = "102 MOVE";
        public const string msgTurnRight = "104 TURN RIGHT";
        public const string msgPickUp = "105 GET MESSAGE";
        public const string msgLogout = "106 LOGOUT";
        public const string msgKeyRequest = "107 KEY REQUEST";
        public const string msgOK = "200 OK";

        //Client messages
        public const string msgRecharing = "RECHARGING";
        public const string msgFullPower = "FULL POWER";

        //Error messages
        public const string msgErrorCode = "234MSGTooLong0649";
        public const string msgTimeOutError = "TookYouLongEnough39434";
        public const string msgLoginFailed = "300 LOGIN FAILED";
        public const string msgSyntaxError = "301 SYNTAX ERROR";
        public const string msgLogicError = "302 LOGIC ERROR";
        public const string msgKeyOutOfRange = "303 KEY OUT OF RANGE";
    }
}
