using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoException
    {
        private const string _textSeparator = "*********************************************";
        private const string Separator = "_______________________________________________";

        public static string ToString(Exception exception)
        {
            return ToString(exception, null);
        }

        public static string ToString(Exception exception, NameValueCollection additionalInfo)
        {
            // Create StringBuilder to maintain publishing information.
            StringBuilder info = new StringBuilder();

            #region Record the contents of the AdditionalInfo collection
            // Record the contents of the AdditionalInfo collection.
            if (additionalInfo != null)
            {
                // Record General information.
                info.AppendFormat("{0}General Information {0}{1}{0}Additional Info:", Environment.NewLine, _textSeparator);

                foreach (string i in additionalInfo)
                {
                    info.AppendFormat("{0}{1}: {2}", Environment.NewLine, i, additionalInfo.Get(i));
                }
            }
            #endregion

            if (exception == null)
            {
                info.AppendFormat("{0}{0}No Exception object has been provided.{0}", Environment.NewLine);
            }
            else
            {
                #region Loop through each exception class in the chain of exception objects
                // Loop through each exception class in the chain of exception objects.
                Exception currentException = exception;	// Temp variable to hold InnerException object during the loop.
                int intExceptionCount = 1;				// Count variable to track the number of exceptions in the chain.
                do
                {
                    // Write title information for the exception object.
                    info.AppendFormat("{0}{0}{1}) Exception Information{0}{2}", Environment.NewLine, intExceptionCount.ToString(), _textSeparator);
                    info.AppendFormat("{0}Exception Type: {1}", Environment.NewLine, currentException.GetType().FullName);

                    #region Loop through the public properties of the exception object and record their value
                    // Loop through the public properties of the exception object and record their value.
                    PropertyInfo[] aryPublicProperties = currentException.GetType().GetProperties();
                    NameValueCollection currentAdditionalInfo;
                    foreach (PropertyInfo p in aryPublicProperties)
                    {
                        // Do not log information for the InnerException or StackTrace. This information is 
                        // captured later in the process.
                        if (p.Name != "InnerException" && p.Name != "StackTrace")
                        {
                            if (p.GetValue(currentException, null) == null)
                            {
                                info.AppendFormat("{0}{1}: NULL", Environment.NewLine, p.Name);
                            }
                            else
                            {
                                // Loop through the collection of AdditionalInformation if the exception type is a ChoAApplicationException.
                                if (p.Name == "AdditionalInformation" /* && currentException is ChoApplicationException */)
                                {
                                    // Verify the collection is not null.
                                    if (p.GetValue(currentException, null) != null)
                                    {
                                        // Cast the collection into a local variable.
                                        currentAdditionalInfo = (NameValueCollection)p.GetValue(currentException, null);

                                        // Check if the collection contains values.
                                        if (currentAdditionalInfo.Count > 0)
                                        {
                                            info.AppendFormat("{0}AdditionalInformation:", Environment.NewLine);

                                            // Loop through the collection adding the information to the string builder.
                                            for (int i = 0; i < currentAdditionalInfo.Count; i++)
                                            {
                                                info.AppendFormat("{0}{1}: {2}", Environment.NewLine, currentAdditionalInfo.GetKey(i), currentAdditionalInfo[i]);
                                            }
                                        }
                                    }
                                }
                                // Otherwise just write the ToString() value of the property.
                                else
                                {
                                    info.AppendFormat("{0}{1}: {2}", Environment.NewLine, p.Name, p.GetValue(currentException, null));
                                }
                            }
                        }
                    }
                    #endregion
                    #region Record the Exception StackTrace
                    // Record the StackTrace with separate label.
                    if (currentException.StackTrace != null)
                    {
                        info.AppendFormat("{0}{0}StackTrace Information{0}{1}", Environment.NewLine, _textSeparator);
                        info.AppendFormat("{0}{1}", Environment.NewLine, currentException.StackTrace);
                    }
                    #endregion

                    // Reset the temp exception object and iterate the counter.
                    currentException = currentException.InnerException;
                    intExceptionCount++;
                } while (currentException != null);
                #endregion
            }

            info.AppendFormat("{0}{1}", Environment.NewLine, Separator);

            return info.ToString();
        }
    }
}
