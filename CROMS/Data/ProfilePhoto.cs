using System;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>
    /// The signed-in user's optional profile photo (<c>staff_biodata.profile_photo</c>). Purely
    /// self-service: nothing here invents or requires a photo — every place it's drawn falls
    /// back to the person's initials (see <see cref="Modules.AvatarPainter"/>) when this
    /// returns null. Upserts into staff_biodata so a photo can be saved even for a user whose
    /// work-detail row an admin has never created.
    /// </summary>
    public static class ProfilePhoto
    {
        public static byte[] Load(int userId)
        {
            var dt = Db.Pull("SELECT profile_photo FROM staff_biodata WHERE user_id=@id",
                new MySqlParameter("@id", userId));
            if (dt.Rows.Count == 0 || dt.Rows[0]["profile_photo"] == DBNull.Value) return null;
            return (byte[])dt.Rows[0]["profile_photo"];
        }

        public static void Save(int userId, byte[] bytes, string fileName)
        {
            var img = new MySqlParameter("@img", MySqlDbType.LongBlob) { Value = (object)bytes ?? DBNull.Value };
            Db.Push(
                "INSERT INTO staff_biodata (user_id, profile_photo, profile_photo_name) VALUES (@id, @img, @fn) " +
                "ON DUPLICATE KEY UPDATE profile_photo=@img, profile_photo_name=@fn",
                new MySqlParameter("@id", userId), img, new MySqlParameter("@fn", (object)fileName ?? DBNull.Value));
        }

        public static void Remove(int userId) => Save(userId, null, null);
    }
}
