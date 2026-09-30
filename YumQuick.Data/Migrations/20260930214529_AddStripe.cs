using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YumQuick.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStripe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FawryRefNumber",
                table: "Orders",
                newName: "StripePaymentIntentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StripePaymentIntentId",
                table: "Orders",
                newName: "FawryRefNumber");
        }
    }
}
