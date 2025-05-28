namespace GuiApplication;

using System;
using System.Drawing;
using System.Windows.Forms;
using SMBLibrary;
using SMBLibrary.Client;
using System.Net;
using System.CodeDom;

public class MainForm : Form
{
    private TextBox timeoutTextBox;
    private TextBox bufferSizeTextBox;
    private RichTextBox logTextBox;
    private Button startButton;
    private Button stopButton;
    private Button exitButton;
    private const int TIMEOUT = 300000; // Connection timeout ensures that the connection to the server ends after timeout
    private const int BUFFER_SIZE = 8192; // Buffer size for returned change data, which will be used in ChangeNotifyRequest
    private ISMBClient m_client;
    private ISMBFileStore m_fileStore;
    private object? m_ioRequest;
    private const string serverIP = "127.0.0.1"; // sharing a local folder on the same laptop to test SMBLibrary
    private const string shareName = "MySharedFolder"; // this folder is under C:\Temp and shared in windows 11
    private const string domain = "azured"; // domain, if necessary
    private const string username = "yasinalakese";
    private const string password = ""; // if "Jeder" is set in properties of the shared folder, then we do not need a password

    public MainForm()
    {
        InitializeComponent();
    }

    private enum LogLevel
    {
        Info,
        Success,
        Error,
        Action
    }

    private void InitializeComponent()
    {
        this.Text = "MyApp";
        this.MinimumSize = new Size(400, 300);

        var timeoutLabel = new Label
        {
            Text = "Timeout:",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };

        timeoutTextBox = new TextBox
        {
            Text = Convert.ToString(TIMEOUT),
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Width = 100
        };

        var bufferSizeLabel = new Label
        {
            Text = "Buffer Size:",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };

        bufferSizeTextBox = new TextBox
        {
            Text = Convert.ToString(BUFFER_SIZE),
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Width = 100
        };

        logTextBox = new RichTextBox
        {
            Multiline = true,
            ScrollBars = RichTextBoxScrollBars.Both,
            WordWrap = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Height = 100,
            ReadOnly = true
        };

        startButton = new Button
        {
            Text = "Start",
            AutoSize = true,
            Anchor = AnchorStyles.Right
        };
        startButton.Click += (s, e) => StartApplication();

        stopButton = new Button
        {
            Text = "Stop",
            AutoSize = true,
            Anchor = AnchorStyles.Right
        };
        stopButton.Click += (s, e) => StopApplication();

        exitButton = new Button
        {
            Text = "Exit",
            AutoSize = true,
            Anchor = AnchorStyles.Right
        };
        exitButton.Click += (s, e) => ExitApplication();

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            AutoSize = true
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        table.Controls.Add(timeoutLabel, 0, 0);
        table.Controls.Add(timeoutTextBox, 1, 0);

        table.Controls.Add(bufferSizeLabel, 0, 1);
        table.Controls.Add(bufferSizeTextBox, 1, 1);

        table.Controls.Add(new Label { Text = "Log:", AutoSize = true }, 0, 2);
        table.SetColumnSpan(logTextBox, 2);
        table.Controls.Add(logTextBox, 0, 3);

        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            AutoSize = true
        };
        buttonPanel.Controls.Add(exitButton);
        buttonPanel.Controls.Add(stopButton);
        buttonPanel.Controls.Add(startButton);

        table.SetColumnSpan(buttonPanel, 2);
        table.Controls.Add(buttonPanel, 0, 4);

        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        this.Controls.Add(table);
    }

    private void StartApplication()
    {
        try
        {
            Log("Starting the application...");
            /* Test change notify request */
            this.SendChangeNotify(serverIP, shareName, domain, username, password);
        }
        catch (Exception ex)
        {
            Log("An error occurred: " + ex.Message, LogLevel.Error);
        }
    }

    private void StopApplication()
    {
        try
        {
            this.SendCancelRequest();
        }
        catch (Exception ex)
        {
            Log("An error occurred: " + ex.Message, LogLevel.Error);
        }
    }

    private void ExitApplication()
    {
        Shutdown();
        Application.Exit();
    }

    private void Log(string message, LogLevel level = LogLevel.Info)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => Log(message, level)));
            return;
        }
        if (logTextBox.TextLength > 0)
            logTextBox.AppendText(Environment.NewLine);
        logTextBox.SelectionStart = logTextBox.TextLength;
        logTextBox.SelectionLength = 0;
        switch (level)
        {
            case LogLevel.Error:
                logTextBox.SelectionColor = Color.Red;
                break;
            case LogLevel.Success:
                logTextBox.SelectionColor = Color.Green;
                break;
            case LogLevel.Action:
                logTextBox.SelectionColor = Color.Blue;
                break;
            default:
                logTextBox.SelectionColor = Color.Black;
                break;
        }
        logTextBox.AppendText(message);
        logTextBox.SelectionColor = logTextBox.ForeColor; // reset color
    }

    private void SendChangeNotify(string serverIP, string shareName, string domain, string username, string password)
    {
        // Connect to the server
        this.ConnectToServer(serverIP, shareName, domain, username, password);

        // Start monitoring
        SMB2FileStore smb2FileStore = m_fileStore as SMB2FileStore;
        if (smb2FileStore != null)
        {
            NTStatus notifyStatus = smb2FileStore.StartMonitoring(
                out m_ioRequest,
                "", // Path to the root directory of the share
                NotifyChangeFilter.FileName | NotifyChangeFilter.DirName | NotifyChangeFilter.Size,
                true,
                int.Parse(bufferSizeTextBox.Text),
                OnNotifyChangeCompleted);

            if (notifyStatus == NTStatus.STATUS_NOT_SUPPORTED)
                throw new Exception("Monitoring is not supported by the server: " + notifyStatus);

            if (notifyStatus != NTStatus.STATUS_PENDING)
                throw new Exception("Monitoring could not be started on the server: " + notifyStatus);

            Log("Server started monitoring");
        }
        else
        {
            throw new Exception("m_fileStore is not an instance of SMB2FileStore");
        }
    }

    /**
     * SendChangeNotify
     *
     * Connects to the SMB server, opens a directory handle, and initiates monitoring for file system changes
     * using the SMB protocol's NotifyChange method. This method demonstrates how to set up continuous monitoring
     * of a shared folder for changes like file/folder creation, deletion, or modification.
     * 
     * Key Steps:
     * 1. Connect to the server using ConnectToServer
     * 2. Open a directory handle with GENERIC_READ and DIRECTORY access.
     * 3. Parse user-defined buffer size for change notifications.
     * 4. Subscribe to NotifyChangeEvent to receive notifications.
     * 5. Call NotifyChange to start monitoring.
     * 
     * @param serverIP IP address or hostname of the SMB server.
     * @param shareName Name of the shared folder to monitor
     * @param domain Domain for authentication (or empty for local accounts).
     * @param username Username for authentication.
     * @param password Password for authentication.
     */
    private void SendChangeNotifyOld(string serverIP, string shareName, string domain, string username, string password)
    {
        // Connect to the server
        this.ConnectToServer(serverIP, shareName, domain, username, password);

        // Open a handle to the root directory of the share
        object directoryHandle;
        FileStatus fileStatus;
        NTStatus createStatus = m_fileStore.CreateFile(out directoryHandle,
                                                out fileStatus,
                                                "", // Path to the root directory of the share. This should be empty string. 
                                                AccessMask.GENERIC_READ,
                                                SMBLibrary.FileAttributes.Directory,
                                                ShareAccess.Read | ShareAccess.Write,
                                                CreateDisposition.FILE_OPEN,
                                                CreateOptions.FILE_DIRECTORY_FILE,
                                                null);
        if (createStatus != NTStatus.STATUS_SUCCESS)
            throw new Exception("CreateFile failed: " + createStatus);

        // Parse buffer size from user input (must be a power of two for SMB compatibility)
        if (!int.TryParse(bufferSizeTextBox.Text, out int bufferSize))
        {
            MessageBox.Show("Invalid buffer size value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Subscribe to NotifyChangeEvent for change notifications
        SMB2FileStore smb2FileStore = m_fileStore as SMB2FileStore;
        if (smb2FileStore != null)
        {
            // Ensure event handler is assigned before starting monitoring
            smb2FileStore.NotifyChangeEvent += OnNotifyChangeCompleted;
        }
        else
        {
            throw new Exception("m_fileStore is not an instance of SMB2FileStore");
        }

        // Start monitoring with NotifyChange
        NTStatus notifyStatus = m_fileStore.NotifyChange(
            out m_ioRequest,              // Tracks the request
            directoryHandle,              // Handle to the opened directory
            NotifyChangeFilter.FileName | // Monitor file name changes (create/delete/rename)
            NotifyChangeFilter.DirName |  // Monitor subdirectory name changes
            NotifyChangeFilter.Size,      // Monitor file size changes
            true,                         // Recursively monitor subdirectories
            bufferSize,                   // Buffer size (e.g., 1024, 4096, 8192)
            null,                         // Callback (null because we use events instead. If a callback version is needed, then enter a method like "this.OnNotifyChangeCompleted,")
            new { Client = m_client, FileStore = m_fileStore, DirectoryHandle = directoryHandle } // Context for reissuing requests or cleanup
        );

        // Handle common error scenarios
        if (notifyStatus == NTStatus.STATUS_NOT_SUPPORTED)
            throw new Exception("Monitoring is not supported by the server: " + notifyStatus);

        if (notifyStatus != NTStatus.STATUS_PENDING)
            throw new Exception("Monitoring could not be started on the server: " + notifyStatus);

        Log("Server started monitoring");
    }
    /**
     * OnNotifyChangeCompleted
     * 
     * Callback method invoked when the server responds to a ChangeNotifyRequest (e.g., when a file or directory change is detected).
     * This method can be used either as an event handler for `NotifyChangeEvent` or as a direct callback.
     * 
     * The raw `buffer` contains the SMB2 ChangeNotify response structured as follows:
     * 
     * 1. **SMB2 Header (64 bytes)**  
     *    - Standard SMB2 protocol header.
	 *    - not used in this method, so it will be skipped
     * 
     * 2. **Notify Response Header (8 bytes)**  
     *    - StructureSize (2 bytes): Size of this header (always 0x0008).
     *    - OutputBufferOffset (2 bytes): Offset (from start of SMB2 header) to the change data.
     *    - OutputBufferLength (4 bytes): Length of the change data.
     * 
     * 3. **Change Data (variable length)**  
     *    - One or more `FILE_NOTIFY_INFORMATION` entries, each representing a change on monitored directory/file:
     *      - NextEntryOffset (4 bytes): Offset to the next entry (0 if this is the last entry).
     *      - Action (4 bytes): Type of change (e.g., FILE_ACTION_ADDED, FILE_ACTION_REMOVED).
     *      - FileNameLength (4 bytes): Length of the Unicode filename (in bytes).
     *      - FileName (variable): Unicode filename (null-terminated).
     * 
     * @param status NTStatus.SUCCESS if changes were detected; an error code (e.g., STATUS_NOTIFY_CLEANUP) if the server terminated the request.
     * @param buffer Raw response buffer containing change notifications (null if an error occurred).
     * @param context Context object passed during NotifyChange registration (contains FileStore, DirectoryHandle, etc.).
     */
    private void OnNotifyChangeCompleted(NTStatus status, byte[] buffer, object context)
    {
        if (status != NTStatus.STATUS_SUCCESS)
        {
            Log($"Error: {status}");
            return;
        }

        // Skip SMB2 header (64 bytes) and Notify Response header (8 bytes)
        int offset = 64 + 8; // 72 bytes total
        try
        {
            while (offset < buffer.Length)
            {
                using (var reader = new BinaryReader(new MemoryStream(buffer, offset, buffer.Length - offset)))
                {
                    // Now at this point the buffer contains the FILE_NOTIFY_INFORMATION
                    // Parse FILE_NOTIFY_INFORMATION entry
                    uint nextEntryOffset = reader.ReadUInt32();
                    uint action = reader.ReadUInt32();
                    uint fileNameLength = reader.ReadUInt32();
                    byte[] fileNameBytes = reader.ReadBytes((int)fileNameLength);
                    string fileName = System.Text.Encoding.Unicode.GetString(fileNameBytes).TrimEnd('\0');

                    // Log the change action and filename
                    Log($"> Action: {(FileAction)action}, File: {fileName}", LogLevel.Action);

                    // Move to the next entry (if any). There may me more than one FILE_NOTIFY_INFORMATION like if a file
                    // is renamed, then there are 2 notify information: one for the old name and a second one for the new name
                    if (nextEntryOffset == 0)
                        break;

                    offset += (int)nextEntryOffset;
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to parse buffer: {ex.Message}", LogLevel.Error);
        }
    }

    /**
     * ConnectToServer
     *
     * Establishes an SMB connection to a remote server, authenticates the user, and connects to a shared folder.
     * This method initializes the SMB client, performs login, and tree connection to the specified share.
     * 
     * @param serverIP IP address or hostname of the SMB server.
     * @param shareName Name of the shared folder to connect to (e.g., "C$").
     * @param domain Domain for authentication (or empty for local accounts).
     * @param username Username for authentication.
     * @param password Password for authentication.
     * 
     * @throws Exception If any step (connect, login, tree connect) fails.
     * 
     * Notes:
     * - This method assumes the existence of UI elements (timeoutTextBox, Log method).
     * - The connected SMB2Client and ISMBFileStore are stored in m_client and m_fileStore.
     */
    private void ConnectToServer(string serverIP, string shareName, string domain, string username, string password)
    {
        // Parse timeout value from UI input (used for connection attempts).
        if (!int.TryParse(timeoutTextBox.Text, out int timeout))
        {
            MessageBox.Show("Invalid timeout value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Create a new SMB2Client instance and connect to the server.
        SMB2Client client = new SMB2Client();
        if (!client.Connect(IPAddress.Parse(serverIP), SMBTransportType.DirectTCPTransport, timeout))
            throw new Exception("Failed to connect to server.");

        // Authenticate the user via SMB login.
        NTStatus status = client.Login(domain, username, password);
        if (status != NTStatus.STATUS_SUCCESS)
            throw new Exception("Login failed: " + status);

        // Connect to the specified share using TreeConnect.
        ISMBFileStore fileStore = client.TreeConnect(shareName, out status);
        if (status != NTStatus.STATUS_SUCCESS)
            throw new Exception("TreeConnect failed: " + status);

        // Store the client and file store references in member variables.
        m_client = client;
        m_fileStore = fileStore;
        Log("Connection was successfull", LogLevel.Success);
    }

    /**
     * Shutdown
     *
     * Gracefully disconnects from the SMB server, logs off the session, and releases resources.
     * This method ensures proper cleanup of the SMB connection and associated objects.
     * 
     * Notes:
     * - This method should be called to avoid resource leaks (e.g., open handles, network connections).
     * - Errors during cleanup (e.g., network failures) are not explicitly handled here.
     */
    private void Shutdown()
    {
        Log("Shutting down SMB client...");
        try
        {
            if (m_fileStore != null)
                m_fileStore.Disconnect(); // Disconnect file store (e.g., cancel ChangeNotify requests)
            if (m_client != null)
            {
                m_client.Logoff(); // Log off SMB session
                m_client.Disconnect(); // Disconnect TCP connection
            }
        }
        catch (Exception ex)
        {
            Log($"Something went wrong while cleaning up: {ex.Message}", LogLevel.Error);
        }
        Log("SMB client stopped");
    }

    /**
     * SendCancelRequest
     *
     * Sends a cancellation request to stop directory monitoring initiated by NotifyChange. This method:
     * 1. Calls the SMB library's Cancel method to terminate the server-side monitoring request.
     * 2. Unsubscribes the NotifyChangeEvent handler to prevent memory leaks.
     */
    private void SendCancelRequestOld()
    {
        Log("Sending cancel request...");

        if (m_fileStore == null)
            throw new Exception("Something went wrong with fileStore. Can not send cancel request!");

        // Send SMB CANCEL request to the server
        NTStatus cancelRequestStatus = m_fileStore.Cancel(m_ioRequest);
        if (cancelRequestStatus != NTStatus.STATUS_SUCCESS)
            throw new Exception("Cancel request could not be processed: " + cancelRequestStatus);

        // Unsubscribe from NotifyChangeEvent to prevent memory leaks
        SMB2FileStore smb2FileStore = m_fileStore as SMB2FileStore;
        if (smb2FileStore != null)
        {
            smb2FileStore.NotifyChangeEvent -= OnNotifyChangeCompleted;
        }
        else
        {
            throw new Exception("m_fileStore is not an instance of SMB2FileStore");
        }

        Log("Cancel request was sent successfully", LogLevel.Success);
    }

    private void SendCancelRequest()
    {
        SMB2FileStore smb2FileStore = m_fileStore as SMB2FileStore;
        if (smb2FileStore != null)
        {
            NTStatus stopStatus = smb2FileStore.StopMonitoring(m_ioRequest, OnNotifyChangeCompleted);

            if (stopStatus != NTStatus.STATUS_SUCCESS)
                throw new Exception("Monitoring could not be stopped on the server: " + stopStatus);

            Log("Server stopped monitoring");
        }
        else
        {
            throw new Exception("m_fileStore is not an instance of SMB2FileStore");
        }
    }
}

