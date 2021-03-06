﻿using Mono.Options;
using System;
using System.Collections.Generic;


namespace Simulator
{
    class Program
    {
        static void Main(string[] args)
        {
            //
            // Setup default parameter values.
            //
            string ip = "127.0.0.1";
            int port = 1680;
            bool showHelp = false;

            //
            // Parse command-line arguments.
            //
            var ipHelp = String.Format("udp packets will be sent to this ip address (defaults to {0})", ip);
            var portHelp = String.Format("udp packets will be sent to this port (defaults to {0})", port);

            var options = new OptionSet {
                { "i|ip=", ipHelp, arg => ip = arg },
                { "p|port=", portHelp, (int arg) => port = arg },
                { "h|help", "show this message and exit", arg => showHelp = arg != null },
            };

            List<string> extra;
            try
            {
                // parse the command line
                extra = options.Parse(args);
                if (extra.Count > 0)
                {
                    throw new OptionException("Invalid option", extra[0]);
                }
            }
            catch (OptionException e)
            {
                // Print error message
                var name = String.Format("{0}", System.AppDomain.CurrentDomain.FriendlyName);
                Console.Write(String.Format("{0}: ", name));
                Console.WriteLine(e.Message);
                Console.WriteLine(
                    String.Format("Try `dotnet {0}.dll --help' for more information.",
                                  name));

                // TODO: return error code here.
                return;
            }

            if (showHelp)
            {
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);

                // TODO: return error code here.
                return;
            }

            //
            // Run the Read-Eval-Print-Loop (REPL).
            //
            REPL(ip, port);
        }


        static void REPL(string ip, int port)
        {
            //
            // Run the Read-Eval-Print-Loop (REPL).
            //
            Console.WriteLine("Welcome to the PacketForwarder Simulator");
            Console.WriteLine(String.Format("Broadcasting to {0}, port {1}.", ip, port));
            Console.WriteLine("");
            Console.WriteLine(
                String.Format("Enter verbatim packet text, a packet number in the range 0..{0} or a blank line to exit.",
                              LoRaTools.PrerecordedPackets.GetPacketCount() - 1));
            Console.WriteLine("");

            LoRaTools.PacketForwarder forwarder = new LoRaTools.PacketForwarder(ip, port);

            while (true)
            {
                // Prompt for packet text.
                Console.Write("packet? ");
                Console.Out.Flush();            // TODO: REVIEW: flush not required on Windows. Check on Linux.
                var line = Console.ReadLine();

                // Exit on blank line.
                if (line.Length == 0)
                {
                    Console.WriteLine("bye");
                    Console.WriteLine("");
                    break;
                }

                // Otherwise, try to determine which packet to broadcast.

                LoRaTools.IPacket packet = null;

                if (LoRaTools.PacketValidator.IsLikelyValidLoRaWanPacket(line))
                {
                    // Input line represents complete text of the packet.
                    // Just build the packet from the input text.
                    packet = new LoRaTools.RecordedPacket(line);
                    Console.WriteLine("  ... broadcasting verbatim packet");
                }
                else
                {
                    Int32 n = 0;
                    if (Int32.TryParse(line, out n))
                    {
                        if (n >= 0 && n < LoRaTools.PrerecordedPackets.GetPacketCount())
                        {
                            // Input line represents a valid pre-recorded packet number.
                            // Look up the pre-recorded packet.
                            packet = LoRaTools.PrerecordedPackets.GetPacket(n);
                            Console.WriteLine(String.Format("  ... broadcasting pre-recorded packet {0}.", n));
                        }
                    }
                }

                if (packet != null)
                {
                    // We have a packet. Broadcast it.
                    forwarder.Send(packet);

                    // TODO: REVIEW: should we print out or log the raw packet
                    // text here for diagnostic purposes.
                }
                else
                {
                    // We couldn't figure out which packet was requested.
                    // Print help message.
                    Console.WriteLine("Invalid packet.");
                    PrintREPLHelp();
                }

                // TODO: catch and handle other errors here? At least set return code on failure.
            }
        }


        static void PrintREPLHelp()
        {
            Console.WriteLine("Valid options include");
            Console.WriteLine("  Complete LoRaWan packet text, starting with 24 hex digits");
            Console.WriteLine("     followed by JSON parameters and payload.");
            Console.WriteLine(
                String.Format("  Pre-recorded packet number in the range 0..{0}",
                                LoRaTools.PrerecordedPackets.GetPacketCount() - 1));
            Console.WriteLine("  Blank line to exit");
            Console.WriteLine("");
        }
    }
}
