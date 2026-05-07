using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicAppointmentBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordSaltToPatient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "PasswordSalt",
                table: "Patients",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordSalt",
                table: "Patients");
        }
    }
}
