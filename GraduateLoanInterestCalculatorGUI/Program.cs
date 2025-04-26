// This part is usually in a separate Program.cs file

using GraduateLoanInterestCalculatorGUI;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Standard WinForms application setup
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        // Create and run the main form
        Application.Run(new LoanCalculatorForm());
    }
}