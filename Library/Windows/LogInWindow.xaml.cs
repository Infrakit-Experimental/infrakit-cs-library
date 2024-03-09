using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using static Library.Utils;

namespace Library.InputWindows
{
    /// <summary>
    /// A window that allows users to log in to the system.
    /// </summary>
    public partial class LogInWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogInWindow"/> class.
        /// </summary>
        public LogInWindow()
        {
            /// Sets the general resource dictionary.
            this.setLibraryRDict();

            /// Initializes the components of the window.
            InitializeComponent();

            /// Read the username
            #region username

            try
            {
                var protectedUsername = Settings.get("username");

                /// Unprotects the username from to the value stored in the settings file and set it into the username text box.
                this.tbxUsername.Text = Utils.unprotectData(protectedUsername);
            }
            catch (Exception ex)
            {
                Log.write("logIn.username: " + ex.GetType() + " | " + ex.Message);

                Settings.@override("username");

                var languages = LibraryUtils.getRDict();
                MessageBox.Show(
                    languages["logIn.error.message"].ToString(),
                    languages["logIn.error.caption"].ToString(),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                this.tbxUsername.Text = "";
            }

            #endregion username

            /// Read the password
            #region password

            try
            {
                var protectedPassword = Settings.get("password");

                /// Unprotects the password from to the value stored in the settings file and set it into the password text box.
                this.tbxPassword.Password = Utils.unprotectData(protectedPassword);
            }
            catch (Exception ex)
            {
                Log.write("logIn.password: " + ex.GetType() + " | " + ex.Message);

                Settings.@override("password");

                var languages = LibraryUtils.getRDict();
                MessageBox.Show(
                    languages["logIn.error.message"].ToString(),
                    languages["logIn.error.caption"].ToString(),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }

            #endregion password

            /// Sets the checked state of the remember me checkbox to true if the username text box is not empty.
            this.cbRemeberMe.IsChecked = (this.tbxUsername.Text.Length > 0);

            /// Sets the focus to the username text box.
            this.tbxUsername.Focus();
        }

        #region listeners

        /// <summary>
        /// Handles the click event of the log in button.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void btnLogIn_Click(object sender, RoutedEventArgs e)
        {
            if (!API.setAPIKey(this.tbxUsername.Text, this.tbxPassword.Password)) return;

            if (this.cbRemeberMe.IsChecked == true)
            {
                var protectedUsername = Utils.protectData(this.tbxUsername.Text);
                if (protectedUsername is not null)
                {
                    Settings.set("username", protectedUsername);
                }
                else
                {
                    Settings.set("username");
                }

                var protectedPassword = Utils.protectData(this.tbxPassword.Password);
                if(protectedPassword is not null)
                {
                    Settings.set("password", protectedPassword);
                }
                else
                {
                    Settings.set("password");
                }
            }
            else
            {
                Settings.set("username");
                Settings.set("password");
            }

            Log.write("log.logIn \"" + this.tbxUsername.Text + "\"");
            Utils.activeUser = this.tbxUsername.Text;
            this.DialogResult = true;
            this.Close();

            return;
        }

        /// <summary>
        /// Handles the key down event of the window.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                this.btnLogIn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }

        #endregion listeners
    }
}
