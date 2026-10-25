using System;
using System.Drawing;
using System.Windows.Forms;

namespace FerreteriaTornilloFeliz.Presentacion
{
    /// <summary>
    /// Tema visual del sistema, tomado de los colores del logo (naranja, ámbar y marrón oscuro).
    /// Solo cambia la apariencia: no toca la lógica ni la base de datos.
    /// Uso: Tema.Aplicar(this) al final de CrearControles() de cada formulario.
    /// </summary>
    public static class Tema
    {
        public static readonly Color Carbon = Color.FromArgb(43, 33, 24);
        public static readonly Color Marron = Color.FromArgb(91, 70, 54);
        public static readonly Color Naranja = Color.FromArgb(245, 159, 0);
        public static readonly Color Ambar = Color.FromArgb(255, 197, 61);
        public static readonly Color Crema = Color.FromArgb(255, 246, 224);
        public static readonly Color CremaClara = Color.FromArgb(255, 251, 239);
        public static readonly Color Terracota = Color.FromArgb(194, 65, 12);
        public static readonly Color TerracotaOscuro = Color.FromArgb(154, 52, 10);
        public static readonly Color Linea = Color.FromArgb(227, 213, 191);

        /// <summary>Aplica el tema al formulario y a todos sus controles (incluidas las pestañas).</summary>
        public static void Aplicar(Form form)
        {
            form.BackColor = Crema;
            form.ForeColor = Carbon;
            form.Font = new Font("Segoe UI", 9F);
            AplicarA(form);
        }

        /// <summary>Menú oscuro con resaltado marrón y letras blancas.</summary>
        public static void EstiloMenu(MenuStrip menu)
        {
            menu.BackColor = Carbon;
            menu.ForeColor = Color.White;
            menu.Renderer = new ToolStripProfessionalRenderer(new ColoresMenu());
            foreach (ToolStripItem item in menu.Items)
            {
                item.ForeColor = Color.White;
            }
        }

        private static void AplicarA(Control padre)
        {
            foreach (Control c in padre.Controls)
            {
                Button boton = c as Button;
                if (boton != null) EstiloBoton(boton);

                DataGridView tabla = c as DataGridView;
                if (tabla != null) EstiloTabla(tabla);

                TabPage pestana = c as TabPage;
                if (pestana != null)
                {
                    pestana.UseVisualStyleBackColor = false;
                    pestana.BackColor = Crema;
                }

                AplicarA(c);
            }
        }

        // Naranja para acciones principales; terracota para las que quitan o cancelan.
        private static void EstiloBoton(Button b)
        {
            bool secundario = b.Text.StartsWith("Desactivar") || b.Text.StartsWith("Cancelar") || b.Text.StartsWith("Quitar");

            b.UseVisualStyleBackColor = false;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = secundario ? TerracotaOscuro : Ambar;
            b.FlatAppearance.MouseDownBackColor = secundario ? TerracotaOscuro : Naranja;
            b.Cursor = Cursors.Hand;
            b.Font = new Font(b.Font, FontStyle.Bold);

            PintarEstado(b, secundario);
            // Los botones se activan y desactivan según el estado de la pantalla: se repinta en cada cambio.
            b.EnabledChanged += delegate { PintarEstado(b, secundario); };
        }

        private static void PintarEstado(Button b, bool secundario)
        {
            if (b.Enabled)
            {
                b.BackColor = secundario ? Terracota : Naranja;
                b.ForeColor = secundario ? Color.White : Carbon;
            }
            else
            {
                b.BackColor = Color.FromArgb(226, 214, 195);
                b.ForeColor = Color.FromArgb(140, 125, 110);
            }
        }

        private static void EstiloTabla(DataGridView g)
        {
            g.EnableHeadersVisualStyles = false;
            g.BorderStyle = BorderStyle.None;
            g.BackgroundColor = Color.White;
            g.GridColor = Linea;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = 32;
            g.RowTemplate.Height = 26;

            g.ColumnHeadersDefaultCellStyle.BackColor = Carbon;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Carbon;
            g.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            g.DefaultCellStyle.SelectionBackColor = Ambar;
            g.DefaultCellStyle.SelectionForeColor = Carbon;
            g.AlternatingRowsDefaultCellStyle.BackColor = CremaClara;
        }

        // Colores del menú (sin azul del tema de Windows).
        private class ColoresMenu : ProfessionalColorTable
        {
            public override Color MenuStripGradientBegin { get { return Carbon; } }
            public override Color MenuStripGradientEnd { get { return Carbon; } }
            public override Color MenuItemSelected { get { return Marron; } }
            public override Color MenuItemSelectedGradientBegin { get { return Marron; } }
            public override Color MenuItemSelectedGradientEnd { get { return Marron; } }
            public override Color MenuItemPressedGradientBegin { get { return Marron; } }
            public override Color MenuItemPressedGradientMiddle { get { return Marron; } }
            public override Color MenuItemPressedGradientEnd { get { return Marron; } }
            public override Color MenuItemBorder { get { return Naranja; } }
            public override Color MenuBorder { get { return Naranja; } }
        }
    }
}
