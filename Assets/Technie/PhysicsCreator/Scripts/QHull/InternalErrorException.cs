/* C# port of QHull. See 'QHull Licence.txt' for license details */

using System;

namespace Technie.PhysicsCreator.QHull
{
	
	/**
	 * Exception thrown when QHull encounters an internal error.
	 */
	public class InternalErrorException : SystemException
	{
		public InternalErrorException (string msg) : base(msg)
		{
			
		}
	}

} // namespace Technie.PhysicsCreator.QHull
