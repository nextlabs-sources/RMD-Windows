
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace PdfFileAnalyzer
{
public class ProgramState
	{
	public String	ProjectFolder;

	public  static ProgramState	State;
	private static String FileName = "PdfFileAnalyzerState.xml";

	////////////////////////////////////////////////////////////////////
	// Constructor
	////////////////////////////////////////////////////////////////////

	public ProgramState()
		{
		ProjectFolder = String.Empty;
		return;
		}

	////////////////////////////////////////////////////////////////////
	// Copy Constructor
	////////////////////////////////////////////////////////////////////

	public ProgramState
			(
			ProgramState Other
			)
		{
		this.ProjectFolder = Other.ProjectFolder;
		return;
		}

	////////////////////////////////////////////////////////////////////
	// Compare two objects
	////////////////////////////////////////////////////////////////////

	public Boolean IsEqual
			(
			ProgramState Other
			)
		{
		return(this.ProjectFolder == Other.ProjectFolder);
		}

	////////////////////////////////////////////////////////////////////
	// Save Program State
	////////////////////////////////////////////////////////////////////

	public static void SaveState
			(
			ProgramState	NewState
			)
		{
		// test for change
		if(!State.IsEqual(NewState))
			{
			// replace state
			State = NewState;

			// save it
			SaveState();
			}

		// exit
		return;
		}

	////////////////////////////////////////////////////////////////////
	// Save Program State
	////////////////////////////////////////////////////////////////////

	public static void SaveState()
		{
		// create a new program state file
		XmlTextWriter TextFile = new XmlTextWriter(FileName, null);

		// create xml serializing object
		XmlSerializer XmlFile = new XmlSerializer(typeof(ProgramState));

		// serialize the program state
		XmlFile.Serialize(TextFile, State);

		// close the file
		TextFile.Close();

		// exit
		return;
		}

	////////////////////////////////////////////////////////////////////
	// Load Program State
	////////////////////////////////////////////////////////////////////

	public static void LoadState()
		{
		XmlTextReader
			TextFile = null;

		// program state file exist
		if(File.Exists(FileName))
			{
			try
				{
				// read program state file
				TextFile = new XmlTextReader(FileName);

				// create xml serializing object
				XmlSerializer XmlFile = new XmlSerializer(typeof(ProgramState));

				// deserialize the program state
				State = (ProgramState) XmlFile.Deserialize(TextFile);
				}
			catch
				{
				State = null;
				}

			// close the file
			if(TextFile != null) TextFile.Close();
			}

		// we have no program state file
		if(State == null)
			{
			// create new default program state
			State = new ProgramState();

			// save default
			SaveState();
			}

		// exit
		return;
		}
	}
}
