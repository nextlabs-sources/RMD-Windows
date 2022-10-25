using SkydrmLocal.rmc.ui.components.ValiditySpecify.model;
using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SkydrmLocal.rmc.ui.components.ValiditySpecify
{
    /// <summary>
    /// Interaction logic for ValiditySpecify.xaml
    /// </summary>
    public partial class ValiditySpecify : UserControl
    {
        private const string DATE_FORMATTER = "MMMM dd, yyyy";

        private ValiditySpecifyConfig validitySpecifyConfig = new ValiditySpecifyConfig();
        private int years;
        private int months = 1;
        private int weeks = 0;
        private int days;

        private DateTime settedStartDateTime;
        private DateTime settedEndDateTime;
        //for clear button
        private DateTime clearStartDateTime;
        private DateTime clearEndDateTime;

        private DateTime settedRelativeEndDateTime;
        private DateTime settedAbsoluteEndDateTime;
        private DateTime settedRangeStartDateTime;
        private DateTime settedRangeEndDateTime;

        private long relativeStartDateTimeMillis;
        private long relativeEndDateTimeMillis;
        private string today;
        private string another;
        private string rangestart;
        private string rangeend;

        //Those public properties are supported for Relative mode.
        public int Years
        {
            get { return years; }
            set { years = value; }
        }

        public int Months
        {
            get { return months; }
            set { months = value; }
        }
        public int Weeks
        {
            get { return weeks; }
            set { weeks = value; }
        }
        public int Days
        {
            get { return days; }
            set { days = value; }
        }
        public DateTime SettedRelativeEndDateTime
        {
            get { return settedRelativeEndDateTime; }
            set { settedRelativeEndDateTime = value; }
        }

        //The public property below is supported for Absolute Mode.
        public DateTime SettedAbsoluteEndDateTime
        {
            get { return settedAbsoluteEndDateTime; }
            set { settedAbsoluteEndDateTime = value; }
        }
        //Those public two properties are supported for Range Mode.
        public DateTime SettedRangeStartDateTime
        {
            get { return settedRangeStartDateTime; }
            set { settedRangeStartDateTime = value; }
        }
        public DateTime SettedRangeEndDateTime
        {
            get { return settedRangeEndDateTime; }
            set { settedRangeEndDateTime = value; }
        }

        // Notifiy date changed.
        public delegate void DateChangedHandler(bool IsChange);
        public event DateChangedHandler DateChangedEvent;

        public ValiditySpecify()
        {
            InitializeComponent();
        }

        public void doInitial(IExpiry expiry, string expireDatevalue)
        {
            //Data source bind.
            this.DataContext = validitySpecifyConfig;
            //Init start date timillis.
            InitialTimeMillis(expiry, expireDatevalue);
            //this.calendar1.DisplayDateStart = settedStartDateTime;
            //this.calendar1.DisplayDateEnd = settedEndDateTime;
        }

        private void InitialTimeMillis(IExpiry expiry, string expireDatevalue)
        {
            //Get current year,month,day.
            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;
            int day = DateTime.Now.Day;
            //Set start DateTime as 2018-03-20-0:0:0 OR 12:0:0 AM;
            settedStartDateTime = new DateTime(year, month, day, 0, 0, 0);
            clearStartDateTime = settedStartDateTime;
            //Set End DateTime add 1 month make the end like 23:59:59 OR 11:59:59 PM;
            settedEndDateTime = settedStartDateTime.AddYears(Years).AddMonths(Months).AddDays(7 * Weeks + Days - 1).AddHours(23).AddMinutes(59).AddSeconds(59);
            clearEndDateTime = settedEndDateTime;

            relativeStartDateTimeMillis = settedStartDateTime.Ticks;
            relativeEndDateTimeMillis = settedEndDateTime.Ticks;

            SettedRelativeEndDateTime = settedEndDateTime;

            SettedAbsoluteEndDateTime = settedEndDateTime;

            SettedRangeStartDateTime = settedStartDateTime;
            SettedRangeEndDateTime = settedEndDateTime;

            today = settedStartDateTime.ToString(DATE_FORMATTER);

            //for Restore the last operation
            if (expiry != null)
            {
                int opetion = expiry.GetOpetion();
                switch (opetion)
                {
                    case 1://validitySpecifyConfig.ExpiryMode == ExpiryMode.RELATIVE
                        Regex regexRE = new Regex(" To ");
                        string[] dateRe = regexRE.Split(expireDatevalue);
                        relativeStartDateTimeMillis = Convert.ToDateTime(dateRe[0]).Ticks;
                        relativeEndDateTimeMillis = Convert.ToDateTime(dateRe[1]).Ticks;
                        IRelative relative = (IRelative)expiry;
                        years = relative.GetYears();
                        months = relative.GetMonths();
                        weeks = relative.GetWeeks();
                        days = relative.GetDays();
                        validitySpecifyConfig.ExpiryMode = ExpiryMode.RELATIVE;
                        break;
                    case 2://validitySpecifyConfig.ExpiryMode == ExpiryMode.ABSOLUTE_DATE
                        Regex regexAB = new Regex("Until");
                        string[] dateAB = regexAB.Split(expireDatevalue);
                        SettedAbsoluteEndDateTime = (DateTime)GenerateSettedEndDate(Convert.ToDateTime(dateAB[1]));
                        if (SettedAbsoluteEndDateTime < settedStartDateTime)
                        {
                            SettedAbsoluteEndDateTime = (DateTime)GenerateSettedEndDate(settedStartDateTime);
                        }
                        validitySpecifyConfig.ExpiryMode = ExpiryMode.ABSOLUTE_DATE;
                        break;
                    case 3://validitySpecifyConfig.ExpiryMode == ExpiryMode.DATA_RANGE
                        Regex regexDR = new Regex(" To ");
                        string[] dateDR = regexDR.Split(expireDatevalue);
                        SettedRangeStartDateTime = (DateTime)GenerateSettedStartDate(Convert.ToDateTime(dateDR[0]));
                        SettedRangeEndDateTime = (DateTime)GenerateSettedEndDate(Convert.ToDateTime(dateDR[1]));
                        if (SettedRangeStartDateTime < settedStartDateTime)
                        {
                            SettedRangeStartDateTime = settedStartDateTime;
                            SettedRangeEndDateTime = settedEndDateTime;
                        }
                        validitySpecifyConfig.ExpiryMode = ExpiryMode.DATA_RANGE;
                        break;
                    default:
                        break;
                }

            }

        }

        /*
         *Callback of radio button checked.
         */
        private void RadioButton_ModeChecked(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton != null && radioButton.Name != null)
            {
                DateChangedEvent?.Invoke(true);

                switch (radioButton.Name.ToString())
                {
                    case "Radio_Never_Expire":
                        Never_Expire();
                        break;
                    case "Radio_Relative":
                        //Text change will excute TextBox_InputTextChanged event
                        this.yearsTB.Text = years.ToString();
                        this.monthsTB.Text = months.ToString();
                        this.weeksTB.Text = weeks.ToString();
                        this.daysTB.Text = days.ToString();

                        Relative();
                        break;
                    case "Radio_Absolute_Date":
                        Absolute_Date();
                        break;
                    case "Radio_Data_Range":
                        Data_Range();
                        break;
                }
                //SolidColorBrush myBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x27, 0xAE, 0x60));
                //radioButton.Foreground = (bool)radioButton.IsChecked ? myBrush : Brushes.Black;
            }
        }

        private void Never_Expire()
        {
            validitySpecifyConfig.ExpiryMode = ExpiryMode.NEVER_EXPIRE;
            validitySpecifyConfig.ValidityDateValue = "Access rights will ";
        }

        private void Relative()
        {
            validitySpecifyConfig.ExpiryMode = ExpiryMode.RELATIVE;

            long elapsedTicks = relativeEndDateTimeMillis - relativeStartDateTimeMillis;
            TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);

            another = SettedRelativeEndDateTime.ToString(DATE_FORMATTER);

            //Nofify UI changes.
            validitySpecifyConfig.ValidityDateValue = today + " To " + another;
            validitySpecifyConfig.ValidityCountDaysValue = (elapsedSpan.Days + 1).ToString() + " days";
        }

        private void Absolute_Date()
        {
            validitySpecifyConfig.ExpiryMode = ExpiryMode.ABSOLUTE_DATE;
            another = SettedAbsoluteEndDateTime.ToString(DATE_FORMATTER);

            //Nofify UI change.
            validitySpecifyConfig.ValidityDateValue = "Until " + another;
            validitySpecifyConfig.ValidityCountDaysValue = CountDays(settedStartDateTime.Ticks, SettedAbsoluteEndDateTime.Ticks) + " days";

            //Update calendar selected dates.
            this.calendar1.SelectedDate = SettedAbsoluteEndDateTime;
            this.calendar1.DisplayDate = SettedAbsoluteEndDateTime;
            //Update calendar blackout dates.
            UpdateBlackOutDates(this.calendar1, settedStartDateTime);
            
        }

        private void Data_Range()
        {
            validitySpecifyConfig.ExpiryMode = ExpiryMode.DATA_RANGE;
            rangestart = SettedRangeStartDateTime.ToString(DATE_FORMATTER);
            rangeend = SettedRangeEndDateTime.ToString(DATE_FORMATTER);

            validitySpecifyConfig.ValidityDateValue = rangestart + " To " + rangeend;
            validitySpecifyConfig.ValidityCountDaysValue = CountDays(SettedRangeStartDateTime.Ticks, SettedRangeEndDateTime.Ticks) + " days";
            //Update calendar selected dates.
            this.calendar1.SelectedDate = SettedRangeStartDateTime;
            this.calendar1.DisplayDate = SettedRangeStartDateTime;
            this.calendar2.SelectedDate = SettedRangeEndDateTime;
            this.calendar2.DisplayDate = SettedRangeEndDateTime;
            //Update calendar blackout dates.
            UpdateBlackOutDates(this.calendar1, settedStartDateTime);
            UpdateBlackOutDates(this.calendar2, SettedRangeStartDateTime);
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
        }

        private void TextBox_InputTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            DateChangedEvent?.Invoke(true);

            switch (textBox.Name)
            {
                case "yearsTB":
                    if (textBox.Text != null && textBox.Text != "" && textBox.Text != " ")
                    {
                        Years = Convert.ToInt32(textBox.Text);
                    }
                    else
                    {
                        Years = 0;
                    }
                    UpdateRelativeDays(Years, Months, Weeks, Days);
                    break;
                case "monthsTB":
                    if (textBox.Text != null && textBox.Text != "" && textBox.Text != " ")
                    {
                        Months = Convert.ToInt32(textBox.Text);
                    }
                    else
                    {
                        Months = 0;
                    }
                    UpdateRelativeDays(Years, Months, Weeks, Days);
                    break;
                case "weeksTB":
                    if (textBox.Text != null && textBox.Text != "" && textBox.Text != " ")
                    {
                        Weeks = Convert.ToInt32(textBox.Text);
                    }
                    else
                    {
                        Weeks = 0;
                    }
                    UpdateRelativeDays(Years, Months, Weeks, Days);
                    break;
                case "daysTB":
                    if (textBox.Text != null && textBox.Text != "" && textBox.Text != " ")
                    {
                        Days = Convert.ToInt32(textBox.Text);
                    }
                    else
                    {
                        Days = 0;
                    }
                    UpdateRelativeDays(Years, Months, Weeks, Days);
                    break;
            }
        }

        private void UpdateRelativeDays(int years, int months, int weeks, int days)
        {
            if (validitySpecifyConfig.ExpiryMode != ExpiryMode.RELATIVE)
            {
                return;
            }
            //Generate setted end date time.
            if (years == 0 && months == 0 && weeks == 0 && days == 0)
            {
                //MessageBox.Show("Thoes four inputs cannot be null at the same time.");
                this.daysTB.Text = "1";//modify texbox.text will trigger 'TextBox_InputTextChanged' event
                return;
            }
            SettedRelativeEndDateTime = settedStartDateTime.AddYears(years).AddMonths(months).AddDays(7 * weeks + days - 1).AddHours(23).AddMinutes(59).AddSeconds(59);
            relativeEndDateTimeMillis = SettedRelativeEndDateTime.Ticks;
            another = SettedRelativeEndDateTime.ToString(DATE_FORMATTER);

            //Update ui display.
            validitySpecifyConfig.ValidityDateValue = today + " To " + another;
            //Calc ticks between two date time.
            validitySpecifyConfig.ValidityCountDaysValue = CountDays(relativeStartDateTimeMillis, relativeEndDateTimeMillis) + " days";

            //For test
            //Console.WriteLine("start---------:" + settedStartDateTime.ToString());
            //Console.WriteLine("end-----------:" + settedEndDateTime.ToString());
        }

        private void Calendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            Calendar calendar = sender as Calendar;
            if (calendar != null)
            {
                //When select a date in calendar,when you want click the "Select" button.However you need to click the "Select" button twice:
                //once to de-focus the calendar ,and again to actually press it.The mouse leave event does not trigger on the calendar if an 
                //item is selected onside it.
                //Add follow to avoid the situation above when Calendar selected dates changed.
                Mouse.Capture(null);
                switch (calendar.Name)
                {
                    case "calendar1":
                        if (validitySpecifyConfig.ExpiryMode == ExpiryMode.ABSOLUTE_DATE)
                        {
                            DateTime settedSelectedTime = (DateTime)GenerateSettedEndDate((DateTime)calendar.SelectedDate);
                            SettedAbsoluteEndDateTime = settedSelectedTime;

                            another = settedSelectedTime.ToString(DATE_FORMATTER);
                            validitySpecifyConfig.ValidityDateValue = "Until " + another;
                            validitySpecifyConfig.ValidityCountDaysValue = CountDays(settedStartDateTime.Ticks, settedSelectedTime.Ticks) + " days";

                            Console.WriteLine("calendar1 absolute end date: " + settedSelectedTime.ToString());
                        }
                        else
                        {
                            SettedRangeStartDateTime = (DateTime)GenerateSettedStartDate((DateTime)calendar.SelectedDate);
                            rangestart = SettedRangeStartDateTime.ToString(DATE_FORMATTER);
                            if (SettedRangeStartDateTime > SettedRangeEndDateTime)
                            {
                                SettedRangeEndDateTime = SettedRangeStartDateTime;
                                this.calendar2.SelectedDate = SettedRangeEndDateTime;
                                this.calendar2.DisplayDate = SettedRangeEndDateTime;
                                rangeend = SettedRangeEndDateTime.ToString(DATE_FORMATTER);
                                UpdateBlackOutDates(this.calendar2, SettedRangeEndDateTime);
                            }
                            else
                            {
                                UpdateBlackOutDates(this.calendar2, SettedRangeStartDateTime);
                            }
                            Console.WriteLine("calendar1 range start date: " + settedRangeStartDateTime.ToString());
                            UpdateRangeCountDays(SettedRangeStartDateTime, SettedRangeEndDateTime);
                        }
                        break;
                    case "calendar2":
                        SettedRangeEndDateTime = (DateTime)GenerateSettedEndDate((DateTime)calendar.SelectedDate);
                        rangeend = SettedRangeEndDateTime.ToString(DATE_FORMATTER);
                        UpdateRangeCountDays(SettedRangeStartDateTime, SettedRangeEndDateTime);
                        Console.WriteLine("calendar1 range end date: " + settedRangeEndDateTime.ToString());
                        break;
                }
                DateChangedEvent?.Invoke(true);
            }
        }

        private void UpdateRangeCountDays(DateTime start, DateTime end)
        {
            validitySpecifyConfig.ValidityDateValue = start.ToString(DATE_FORMATTER) + " To " + end.ToString(DATE_FORMATTER);
            validitySpecifyConfig.ValidityCountDaysValue = CountDays(start.Ticks, end.Ticks) + " days";
        }

        private int CountDays(long startMillis, long endMillis)
        {
            long elapsedTicks = endMillis - startMillis;
            TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);
            if (elapsedSpan.Days<0)
            {
                return 0;
            }
            return elapsedSpan.Days + 1;
        }

        private DateTime? GenerateSettedStartDate(DateTime targetTime)
        {
            if (targetTime != null)
            {
                return new DateTime(targetTime.Year, targetTime.Month, targetTime.Day).AddHours(0).AddMinutes(0).AddSeconds(0);
            }
            return null;
        }

        private DateTime? GenerateSettedEndDate(DateTime dateTime)
        {
            if (dateTime != null)
            {
                return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day).AddHours(23).AddMinutes(59).AddSeconds(59);
            }
            return null;
        }

        private void UpdateBlackOutDates(Calendar target, DateTime blackoutEndTime)
        {
            if (target == null)
            {
                return;
            }
            target.BlackoutDates.Clear();
            target.BlackoutDates.Add(new CalendarDateRange(DateTime.MinValue, blackoutEndTime.AddDays(-1)));
            //This is not suitable for the situation that the end dates is changeable.
            //target.BlackoutDates.AddDatesInPast();
        }

        public void GetExpireValue(out IExpiry expiry, out string validityContent)
        {
            expiry = null;
            validityContent = CultureStringInfo.ValidityWin_Never_Description2;
            if (validitySpecifyConfig != null)
            {
                switch (validitySpecifyConfig.ExpiryMode)
                {
                    case ExpiryMode.NEVER_EXPIRE:
                        expiry = new NeverExpireImpl();
                        validityContent = CultureStringInfo.ValidityWin_Never_Description2;
                        break;
                    case ExpiryMode.RELATIVE:
                        expiry = new RelativeImpl(years, months, weeks, days);
                        validityContent = today + " To " + another;
                        break;
                    case ExpiryMode.ABSOLUTE_DATE:
                        expiry = new AbsoluteImpl(CommonUtils.DateTimeToTimestamp(settedAbsoluteEndDateTime));
                        validityContent = "Until " + another;
                        break;
                    case ExpiryMode.DATA_RANGE:
                        expiry = new RangeImpl(CommonUtils.DateTimeToTimestamp(settedRangeStartDateTime), CommonUtils.DateTimeToTimestamp(settedRangeEndDateTime));
                        validityContent = rangestart + " To " + rangeend;
                        break;
                }
            }
        }

        //should use  CommonUtils.DateTimeToTimestamp(), should Gets the time zone of the current computer.
        private long GetUnixTimestamp(DateTime target) => (long)target.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;

        private void BtnClear1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            
            if (validitySpecifyConfig.ExpiryMode==ExpiryMode.ABSOLUTE_DATE)//ExpiryMode.ABSOLUTE_DATE
            {
                clearStartDateTime = settedEndDateTime;
            }
            if (validitySpecifyConfig.ExpiryMode == ExpiryMode.DATA_RANGE)
            {
                clearStartDateTime = settedStartDateTime;
            }
            
            this.calendar1.SelectedDate = clearStartDateTime;
            this.calendar1.DisplayDate = clearStartDateTime;
        }

        private void BtnClear2_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Data_Range
            clearEndDateTime = SettedRangeStartDateTime.AddDays(29).AddHours(23).AddMinutes(59).AddSeconds(59);

            this.calendar2.SelectedDate = clearEndDateTime;
            this.calendar2.DisplayDate = clearEndDateTime;
        }
    }

    class NeverExpireImpl : INeverExpire
    {
        public int GetOpetion()
        {
            return 0;
        }
    }

    class RelativeImpl : IRelative
    {
        private int years;
        private int months;
        private int weeks;
        private int days;

        public RelativeImpl(int years, int months, int weeks, int days)
        {
            this.years = years;
            this.months = months;
            this.weeks = weeks;
            this.days = days;
        }
        public int GetOpetion()
        {
            return 1;
        }
        public int GetYears()
        {
            return years;
        }

        public int GetMonths()
        {
            return months;
        }
        public int GetDays()
        {
            return days;
        }
        public int GetWeeks()
        {
            return weeks;
        }
    }

    class AbsoluteImpl : IAbsolute
    {
        private long enddate;
        public AbsoluteImpl(long end)
        {
            this.enddate = end;
        }
        public int GetOpetion()
        {
            return 2;
        }
        public long EndDate()
        {
            return enddate;
        }
    }
    class RangeImpl : IRange
    {
        private long startdate;
        private long enddate;

        public RangeImpl(long start, long end)
        {
            this.startdate = start;
            this.enddate = end;
        }

        public int GetOpetion()
        {
            return 3;
        }
        public long StartDate()
        {
            return startdate;
        }
        public long EndDate()
        {
            return enddate;
        }
    }

}
