using System;
using Microsoft.SPOT;
using GHI.Pins;
using Microsoft.SPOT.Hardware;
using System.Threading;
//using Microsoft.SPOT.IO;
using System.IO;

namespace RoSchmi
{
    public class ButtonNETMF
    {
        InterruptPort input;
        OutputPort led;
        private LedMode currentMode;

        private ButtonEventHandler onButtonEvent;

        /// <summary>Represents the delegate that is used to handle the <see cref="ButtonReleased" /> and <see cref="ButtonPressed" /> events.</summary>
        /// <param name="sender">The <see cref="Button" /> object that raised the event.</param>
        /// <param name="state">The state of the Button</param>
        public delegate void ButtonEventHandler(ButtonNETMF sender, ButtonState state);

        /// <summary>Raised when the button is released.</summary>
        public event ButtonEventHandler ButtonReleased;

        /// <summary>Raised when the button is pressed.</summary>
        public event ButtonEventHandler ButtonPressed;

        /// <summary>Whether or not the button is pressed.</summary>
        public bool Pressed
        {
            get
            {
                return !this.input.Read();
            }
        }

        /// <summary>Whether or not the LED is currently on or off.</summary>
        public bool IsLedOn
        {
            get
            {
                return this.led.Read();
            }
        }

        /// <summary>Gets or sets the LED's current mode of operation.</summary>
        public LedMode Mode
        {
            get
            {
                return this.currentMode;
            }

            set
            {
                this.currentMode = value;

                if (this.currentMode == LedMode.On || (this.currentMode == LedMode.OnWhilePressed && this.Pressed) || (this.currentMode == LedMode.OnWhileReleased && !this.Pressed))
                    this.TurnLedOn();
                else if (this.currentMode == LedMode.Off || (this.currentMode == LedMode.OnWhileReleased && this.Pressed) || (this.currentMode == LedMode.OnWhilePressed && !this.Pressed))
                    this.TurnLedOff();
            }
        }

        /// <summary>The state of the button.</summary>
        public enum ButtonState
        {

            /// <summary>The button is pressed.</summary>
            Pressed = 0,

            /// <summary>The button is released.</summary>
            Released = 1
        }

        /// <summary>The various modes a LED can be set to.</summary>
        public enum LedMode
        {

            /// <summary>The LED is on regardless of the button state.</summary>
            On,

            /// <summary>The LED is off regardless of the button state.</summary>
            Off,

            /// <summary>The LED changes state whenever the button is pressed.</summary>
            ToggleWhenPressed,

            /// <summary>The LED changes state whenever the button is released.</summary>
            ToggleWhenReleased,

            /// <summary>The LED is on while the button is pressed.</summary>
            OnWhilePressed,

            /// <summary>The LED is on except when the button is pressed.</summary>
            OnWhileReleased
        }

        /// <summary>Constructs a new instance.</summary>
		
        public ButtonNETMF(Cpu.Pin intPort, Cpu.Pin buttonLED)
        {
            this.currentMode = LedMode.Off;
            this.input = new InterruptPort(intPort, true, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeBoth);
            this.led = new OutputPort(buttonLED, false);
            this.input.OnInterrupt += input_OnInterrupt;
        }

        void input_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            bool value = data2 != 0;
            var state = value ? ButtonState.Released : ButtonState.Pressed;

            switch (state)
            {
                case ButtonState.Released:
                    if (this.Mode == LedMode.OnWhilePressed)
                        this.TurnLedOff();
                    else if (this.Mode == LedMode.OnWhileReleased)
                        this.TurnLedOn();
                    else if (this.Mode == LedMode.ToggleWhenReleased)
                        this.ToggleLED();

                    break;

                case ButtonState.Pressed:
                    if (this.Mode == LedMode.OnWhilePressed)
                        this.TurnLedOn();
                    else if (this.Mode == LedMode.OnWhileReleased)
                        this.TurnLedOff();
                    else if (this.Mode == LedMode.ToggleWhenPressed)
                        this.ToggleLED();

                    break;
            }

            this.OnButtonEvent(this, state);
        }

        /// <summary>Turns on the LED.</summary>
        public void TurnLedOn()
        {
            this.led.Write(true);
        }

        /// <summary>Turns off the LED.</summary>
        public void TurnLedOff()
        {
            this.led.Write(false);
        }

        /// <summary>Turns the LED off if it is on and on if it is off.</summary>
        public void ToggleLED()
        {
            if (this.IsLedOn)
                this.TurnLedOff();
            else
                this.TurnLedOn();
        }

        private void OnButtonEvent(ButtonNETMF sender, ButtonState state)
        {
            if (this.onButtonEvent == null)
                this.onButtonEvent = this.OnButtonEvent;

            if (state == ButtonState.Pressed)
            {
                this.ButtonPressed(sender, state);
            }
            else
            {
                this.ButtonReleased(sender, state);
            }

            /* The original code from the Gadgeteer module
            if (Program.CheckAndInvoke(state == ButtonState.Released ? this.ButtonReleased : this.ButtonPressed, this.onButtonEvent, sender, state))
            {
                switch (state)
                {
                    case ButtonState.Released: this.ButtonReleased(sender, state); break;
                    case ButtonState.Pressed: this.ButtonPressed(sender, state); break;
                }
            }
            */
        }
    }
}
